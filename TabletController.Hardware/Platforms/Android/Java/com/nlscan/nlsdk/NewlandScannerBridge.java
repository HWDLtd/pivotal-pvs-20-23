package com.nlscan.nlsdk;

import android.content.Context;
import android.util.Log;

/**
 * Bridge class for simplified access to NLDevice scanner functionality from C#.
 * This class wraps the NLDevice SDK and provides callbacks for barcode scans.
 */
public class NewlandScannerBridge {
    private static final String TAG = "NewlandScannerBridge";

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
        // Create device with USB Composite mode (KBW)
        device = new NLDevice(NLDeviceStream.DevClass.DEV_COMPOSITE);
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
