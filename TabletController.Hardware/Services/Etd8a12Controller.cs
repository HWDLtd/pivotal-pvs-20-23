using System;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TabletController.Hardware.Services
{
    public sealed class InputStateChangedEventArgs : EventArgs
    {
        public InputStateChangedEventArgs(bool[] oldState, bool[] newState)
        {
            OldState = oldState ?? throw new ArgumentNullException(nameof(oldState));
            NewState = newState ?? throw new ArgumentNullException(nameof(newState));
        }

        public bool[] OldState { get; }
        public bool[] NewState { get; }
    }

    public sealed class Etd8a12Controller
    {
        private const int RelayCount = 12;
        private const ushort RelayStartRegister = 0x0000;
        private const ushort InputStatusRegister = 0x00C0;
        private const ushort RelayTrueValue = 0x0100;
        private const ushort RelayFalseValue = 0x0200;
        private const ushort RelationRegister = 0x00FA;
        private const ushort RelationUnrelatedValue = 0x0000;
        private const ushort BaudRateRegister = 0x00FE;
        private const ushort BaudRate9600Value = 0x0002;
        private const ushort BaudRate19200Value = 0x0004;
        private static readonly TimeSpan SlowResponseThreshold = TimeSpan.FromMilliseconds(500);
        private const int SendAndValidateTimeoutMs = 800;

        private readonly object _ioLock = new();
        private readonly object _pollLock = new();

        private string _ipAddress = "192.168.0.10";
        private int _port = 5000;
        private byte _unitId = 0x01;

        private ushort _transactionId = 1;
        private bool[]? _lastInputs;
        private CancellationTokenSource? _pollingCts;
        private Task? _pollingTask;

        public event EventHandler<InputStateChangedEventArgs>? InputStateChanged;
        public event EventHandler<Exception>? PollingError;

        public void Configure(string ipAddress, int port, byte unitId)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address is required.", nameof(ipAddress));
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");
            if (unitId == 0 || unitId > 247)
                throw new ArgumentOutOfRangeException(nameof(unitId), "UnitId must be between 1 and 247.");

            _ipAddress = ipAddress;
            _port = port;
            _unitId = unitId;
        }

        public void Start()
        {
            lock (_pollLock)
            {
                if (_pollingTask != null && !_pollingTask.IsCompleted)
                    return;

                _pollingCts = new CancellationTokenSource();
                _pollingTask = Task.Run(() => PollLoopAsync(_pollingCts.Token));
            }
        }

        public void Stop()
        {
            lock (_pollLock)
            {
                if (_pollingCts == null)
                    return;

                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }
        }

        public void SetRelays(bool[] relays)
        {
            if (relays == null)
                throw new ArgumentNullException(nameof(relays));
            if (relays.Length != RelayCount)
                throw new ArgumentException($"Relay array must contain exactly {RelayCount} elements.", nameof(relays));

            ValidateConfiguration();

            ushort[] registers = new ushort[RelayCount];
            for (int i = 0; i < RelayCount; i++)
                registers[i] = relays[i] ? RelayTrueValue : RelayFalseValue;

            lock (_ioLock)
            {
                byte[] frame = BuildWriteMultipleRegistersFrame(_transactionId++, _unitId, RelayStartRegister, registers);
                ExecuteWithReconnect(stream => SendAndValidate(stream, frame));
            }
        }

        public void DisableLinkage()
        {
            ValidateConfiguration();

            lock (_ioLock)
            {
                byte[] frame = BuildWriteSingleRegisterFrame(
                    transactionId: _transactionId++,
                    unitId: _unitId,
                    registerAddress: RelationRegister,
                    value: RelationUnrelatedValue
                );

                ExecuteWithReconnect(stream => SendAndValidate(stream, frame));
            }
        }

        public void SetBaudRate19200()
        {
            ValidateConfiguration();

            lock (_ioLock)
            {
                byte[] frame = BuildWriteSingleRegisterFrame(
                    transactionId: _transactionId++,
                    unitId: _unitId,
                    registerAddress: BaudRateRegister,
                    value: BaudRate19200Value
                );

                ExecuteWithReconnect(stream => SendAndValidate(stream, frame));
            }
        }

        public void SetBaudRate9600()
        {
            ValidateConfiguration();

            lock (_ioLock)
            {
                byte[] frame = BuildWriteSingleRegisterFrame(
                    transactionId: _transactionId++,
                    unitId: _unitId,
                    registerAddress: BaudRateRegister,
                    value: BaudRate9600Value
                );

                ExecuteWithReconnect(stream => SendAndValidate(stream, frame));
            }
        }

        public void SetOutput(int outputNumber, bool state)
        {
            if (outputNumber < 1 || outputNumber > RelayCount)
                throw new ArgumentOutOfRangeException(nameof(outputNumber), $"Output number must be between 1 and {RelayCount}.");

            ValidateConfiguration();

            ushort registerAddress = (ushort)(RelayStartRegister + outputNumber - 1);
            ushort value = state ? RelayTrueValue : RelayFalseValue;

            lock (_ioLock)
            {
                byte[] frame = BuildWriteSingleRegisterFrame(
                    transactionId: _transactionId++,
                    unitId: _unitId,
                    registerAddress: registerAddress,
                    value: value
                );

                ExecuteWithReconnect(stream => SendAndValidate(stream, frame));
            }
        }

        public void PulseOutput(int outputNumber, int durationMs)
        {
            if (outputNumber < 1 || outputNumber > RelayCount)
                throw new ArgumentOutOfRangeException(nameof(outputNumber), $"Output number must be between 1 and {RelayCount}.");
            if (durationMs < 1)
                throw new ArgumentOutOfRangeException(nameof(durationMs), "Duration must be at least 1ms.");

            SetOutput(outputNumber, true);
            Thread.Sleep(durationMs);
            SetOutput(outputNumber, false);
        }

        public bool[] GetInputs()
        {
            ValidateConfiguration();
            ushort word;

            lock (_ioLock)
            {
                word = ReadHoldingRegister(InputStatusRegister);
            }

            bool[] inputs = new bool[RelayCount];
            for (int i = 0; i < RelayCount; i++)
                inputs[i] = (word & (1 << i)) != 0;

            return inputs;
        }

        private async Task PollLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool[] current = GetInputs();
                    bool[]? previous = _lastInputs;

                    if (previous == null)
                    {
                        _lastInputs = current;
                    }
                    else if (!StatesEqual(previous, current))
                    {
                        _lastInputs = current;
                        OnInputStateChanged(Clone(previous), Clone(current));
                    }
                }
                catch (Exception ex)
                {
                    OnPollingError(ex);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(600), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private static bool[] Clone(bool[] source)
        {
            bool[] copy = new bool[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private static bool StatesEqual(bool[] a, bool[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private void OnInputStateChanged(bool[] oldState, bool[] newState)
        {
            InputStateChanged?.Invoke(this, new InputStateChangedEventArgs(oldState, newState));
        }

        private void OnPollingError(Exception ex)
        {
            PollingError?.Invoke(this, ex);
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_ipAddress))
                throw new InvalidOperationException("IP address is not configured.");
            if (_port < 1 || _port > 65535)
                throw new InvalidOperationException("Port is not configured.");
            if (_unitId == 0 || _unitId > 247)
                throw new InvalidOperationException("UnitId is not configured.");
        }

        private static byte[] BuildWriteMultipleRegistersFrame(ushort transactionId, byte unitId, ushort startAddress, ushort[] registers)
        {
            ushort quantity = (ushort)registers.Length;
            byte byteCount = (byte)(quantity * 2);

            byte[] pdu = new byte[1 + 2 + 2 + 1 + byteCount];
            int p = 0;

            pdu[p++] = 0x10; // FC16
            pdu[p++] = (byte)(startAddress >> 8);
            pdu[p++] = (byte)(startAddress & 0xFF);
            pdu[p++] = (byte)(quantity >> 8);
            pdu[p++] = (byte)(quantity & 0xFF);
            pdu[p++] = byteCount;

            foreach (ushort v in registers)
            {
                pdu[p++] = (byte)(v >> 8);
                pdu[p++] = (byte)(v & 0xFF);
            }

            return BuildMbap(transactionId, unitId, pdu);
        }

        private static byte[] BuildWriteSingleRegisterFrame(ushort transactionId, byte unitId, ushort registerAddress, ushort value)
        {
            byte[] pdu =
            {
                0x06,
                (byte)(registerAddress >> 8),
                (byte)(registerAddress & 0xFF),
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };

            return BuildMbap(transactionId, unitId, pdu);
        }

        private static byte[] BuildMbap(ushort transactionId, byte unitId, byte[] pdu)
        {
            ushort length = (ushort)(1 + pdu.Length);

            byte[] frame = new byte[7 + pdu.Length];
            frame[0] = (byte)(transactionId >> 8);
            frame[1] = (byte)(transactionId & 0xFF);
            frame[2] = 0x00; frame[3] = 0x00;
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);
            frame[6] = unitId;

            Buffer.BlockCopy(pdu, 0, frame, 7, pdu.Length);
            return frame;
        }

        private static void SendAndValidate(NetworkStream stream, byte[] frame)
        {
            var stopwatch = Stopwatch.StartNew();
            stream.Write(frame, 0, frame.Length);

            byte[] rx = new byte[256];
            int read = stream.Read(rx, 0, rx.Length);
            stopwatch.Stop();
            LogIfSlow(stopwatch.Elapsed);

            if (read < 9)
                throw new Exception("Modbus TCP response too short.");

            byte fc = rx[7];
            if ((fc & 0x80) != 0)
            {
                byte ex = (read > 8) ? rx[8] : (byte)0;
                throw new Exception($"Modbus exception. FC=0x{fc:X2}, EX=0x{ex:X2}");
            }
        }

        private ushort ReadHoldingRegister(ushort registerAddress)
        {
            return ExecuteWithReconnect(stream =>
            {
                byte[] pdu =
                {
                    0x03,
                    (byte)(registerAddress >> 8), (byte)(registerAddress & 0xFF),
                    0x00, 0x01
                };

                byte[] request = BuildMbap(_transactionId++, _unitId, pdu);
                var stopwatch = Stopwatch.StartNew();
                stream.Write(request, 0, request.Length);

                byte[] response = ReadModbusTcpFrame(stream);
                stopwatch.Stop();
                LogIfSlow(stopwatch.Elapsed);

                byte fc = response[7];
                if ((fc & 0x80) != 0)
                    throw new Exception($"Modbus exception FC=0x{fc:X2} EX=0x{response[8]:X2}");

                if (response[8] != 0x02)
                    throw new Exception($"Unexpected byteCount={response[8]}");

                return (ushort)((response[9] << 8) | response[10]);
            });
        }

        private static byte[] ReadModbusTcpFrame(NetworkStream stream)
        {
            byte[] mbap = ReadExactly(stream, 7);
            ushort length = (ushort)((mbap[4] << 8) | mbap[5]);
            int remaining = length - 1;
            if (remaining < 0)
                throw new Exception("Invalid MBAP length in response.");

            byte[] pdu = ReadExactly(stream, remaining);

            byte[] frame = new byte[7 + remaining];
            Buffer.BlockCopy(mbap, 0, frame, 0, 7);
            Buffer.BlockCopy(pdu, 0, frame, 7, remaining);
            return frame;
        }

        private static byte[] ReadExactly(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    throw new Exception("Socket closed by remote host while reading Modbus response.");

                offset += read;
            }

            return buffer;
        }

        private T ExecuteWithReconnect<T>(Func<NetworkStream, T> action)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    using var client = new TcpClient();
                    client.Connect(_ipAddress, _port);

                    using NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = SendAndValidateTimeoutMs;
                    stream.WriteTimeout = SendAndValidateTimeoutMs;

                    return action(stream);
                }
                catch (Exception ex) when (IsTransportException(ex) && attempt < 2)
                {
                    lastException = ex;
                }
            }

            throw lastException ?? new Exception("Transport error while communicating with device.");
        }

        private void ExecuteWithReconnect(Action<NetworkStream> action)
        {
            ExecuteWithReconnect(stream =>
            {
                action(stream);
                return true;
            });
        }

        private static bool IsTransportException(Exception ex)
        {
            return ex is IOException || ex is SocketException || ex is ObjectDisposedException;
        }

        private static void LogIfSlow(TimeSpan elapsed)
        {
            if (elapsed > SlowResponseThreshold)
                System.Diagnostics.Debug.WriteLine($"Modbus response time {elapsed.TotalMilliseconds:F0}ms exceeds {SlowResponseThreshold.TotalMilliseconds:F0}ms.");
        }
    }
}
