package com.nlscan.nlsdk;

import android.content.Context;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbManager;
import android.util.Log;

import java.util.HashMap;
import java.util.Map;

/**
 * Bridge class for simplified access to NLDevice scanner functionality from C#.
 * This class wraps the NLDevice SDK and provides callbacks for barcode scans.
 */
public class NewlandScannerBridge {
    private static final String TAG = "NewlandScannerBridge";
    private static final int NEWLAND_VID = 0x1EAB;

    private NLDeviceStream device;
    private Context context;
    private boolean isConnected = false;
    private ScannerCallback callback;
    private byte[] barcodeBuffer = new byte[2 * 1024];

    /**
     * Callback interface for scanner events.
     */
    public interface ScannerCallback {
        void onBarcodeScanned(String barcode);
        void onConnectionChanged(boolean connected);
    }

    public NewlandScannerBridge() {
        // Device will be created in open() after auto-detecting the USB mode
        device = null;
    }

    /**
     * Auto-detects the DevClass based on the connected Newland scanner's PID.
     * PID low byte 0x06 = CDC, 0x10 = POS, 0x22 = Composite.
     */
    private NLDeviceStream.DevClass detectDevClass(Context context) {
        UsbManager usbManager = (UsbManager) context.getSystemService(Context.USB_SERVICE);
        if (usbManager == null) {
            Log.w(TAG, "USB Manager not available, defaulting to DEV_COMPOSITE");
            return NLDeviceStream.DevClass.DEV_COMPOSITE;
        }

        HashMap<String, UsbDevice> deviceMap = usbManager.getDeviceList();
        for (Map.Entry<String, UsbDevice> entry : deviceMap.entrySet()) {
            UsbDevice usbdev = entry.getValue();
            if (usbdev.getVendorId() != NEWLAND_VID) continue;

            int lpid = usbdev.getProductId() & 0xFF;
            Log.i(TAG, "Detected Newland device PID=0x" + Integer.toHexString(usbdev.getProductId()) + " (low byte=0x" + Integer.toHexString(lpid) + ")");

            switch (lpid) {
                case 0x06:
                    Log.i(TAG, "Auto-detected USB CDC mode");
                    return NLDeviceStream.DevClass.DEV_CDC;
                case 0x10:
                    Log.i(TAG, "Auto-detected USB POS mode");
                    return NLDeviceStream.DevClass.DEV_POS;
                case 0x22:
                    Log.i(TAG, "Auto-detected USB Composite mode");
                    return NLDeviceStream.DevClass.DEV_COMPOSITE;
                default:
                    Log.w(TAG, "Unknown PID low byte 0x" + Integer.toHexString(lpid) + ", defaulting to DEV_COMPOSITE");
                    return NLDeviceStream.DevClass.DEV_COMPOSITE;
            }
        }

        Log.w(TAG, "No Newland device found, defaulting to DEV_COMPOSITE");
        return NLDeviceStream.DevClass.DEV_COMPOSITE;
    }

    /**
     * Sets the callback for scanner events.
     */
    public void setCallback(ScannerCallback callback) {
        this.callback = callback;
    }

    /**
     * Opens the scanner device and starts listening for barcodes.
     * @param context Android context
     * @return true if device was opened successfully
     */
    public boolean open(Context context) {
        this.context = context;

        try {
            // Auto-detect and create device on each open attempt
            NLDeviceStream.DevClass devClass = detectDevClass(context);
            device = new NLDevice(devClass);

            boolean result = device.nl_OpenDevice(context, new NLDeviceStream.NLUsbListener() {
                @Override
                public void actionUsbPlug(int event) {
                    Log.d(TAG, "USB Plug event: " + event);
                    isConnected = (event == 1);

                    if (callback != null) {
                        callback.onConnectionChanged(isConnected);
                    }

                    if (event == 0) {
                        // Device unplugged
                        isConnected = false;
                    }
                }

                @Override
                public void actionUsbRecv(byte[] recvBuff, int len) {
                    Log.d(TAG, "Received data, length: " + len);

                    if (len > 0 && len <= barcodeBuffer.length) {
                        System.arraycopy(recvBuff, 0, barcodeBuffer, 0, len);

                        // Convert to string using default charset (as per user requirements: checkedItem == 0)
                        String barcode = new String(barcodeBuffer, 0, len);

                        Log.d(TAG, "Barcode scanned: " + barcode);

                        if (callback != null) {
                            callback.onBarcodeScanned(barcode);
                        }
                    }
                }
            });

            if (result) {
                isConnected = true;
                Log.d(TAG, "Scanner opened successfully");

                if (callback != null) {
                    callback.onConnectionChanged(true);
                }
            } else {
                Log.w(TAG, "Failed to open scanner - permission may be pending");
            }

            return result;
        } catch (Exception e) {
            Log.e(TAG, "Error opening scanner: " + e.getMessage(), e);
            return false;
        }
    }

    /**
     * Closes the scanner device.
     */
    public void close() {
        try {
            if (device != null) {
                device.nl_CloseDevice();
                isConnected = false;

                if (callback != null) {
                    callback.onConnectionChanged(false);
                }

                Log.d(TAG, "Scanner closed");
            }
        } catch (Exception e) {
            Log.e(TAG, "Error closing scanner: " + e.getMessage(), e);
        }
    }

    /**
     * Returns whether the device is currently connected.
     */
    public boolean isConnected() {
        return isConnected && device != null && device.nl_DeviceIsOpen();
    }

    /**
     * Triggers a scan programmatically.
     * @return true if scan was triggered
     */
    public boolean triggerScan() {
        try {
            if (device != null && device.nl_DeviceIsOpen()) {
                return device.nl_StartScan();
            }
        } catch (Exception e) {
            Log.e(TAG, "Error triggering scan: " + e.getMessage(), e);
        }
        return false;
    }

    /**
     * Stops an active scan.
     * @return true if scan was stopped
     */
    public boolean stopScan() {
        try {
            if (device != null && device.nl_DeviceIsOpen()) {
                return device.nl_StopScan();
            }
        } catch (Exception e) {
            Log.e(TAG, "Error stopping scan: " + e.getMessage(), e);
        }
        return false;
    }

    /**
     * Gets device information.
     * @return device info string or null
     */
    public String getDeviceInfo() {
        try {
            if (device != null && device.nl_DeviceIsOpen()) {
                return device.nl_GetDeviceInfo();
            }
        } catch (Exception e) {
            Log.e(TAG, "Error getting device info: " + e.getMessage(), e);
        }
        return null;
    }

    /**
     * Gets SDK version.
     * @return SDK version string
     */
    public String getSdkVersion() {
        if (device != null) {
            return device.nl_GetSdkVersion();
        }
        return "Unknown";
    }
}
