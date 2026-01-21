// using System;
// using System.Collections.Generic;
// using System.Threading;
// using libzkfpcsharp;
// using System.Threading.Tasks;

// namespace BiometricStudentPickup.Services
// {
//     // public enum OwnerType
//     // {
//     //     Student,
//     //     Guardian
//     // }
    

//     public class FingerprintService
//     {
//         // private int _nextFingerprintId = 1;
//         private IntPtr _deviceHandle = IntPtr.Zero;
//         private IntPtr _dbHandle = IntPtr.Zero;

//         // private byte[] _fpBuffer;
//         private byte[] _fpBuffer = Array.Empty<byte>();
//         private byte[] _capTemplate = new byte[2048];
//         private int _capTemplateLen = 2048;

//         private bool _isInitialized = false;

//         public FingerprintService()
//         {
//             Initialize();
//         }

//         public void ClearDb()
//         {
//             EnsureReady();
//             zkfp2.DBClear(_dbHandle);
//         }

//         // ============================
//         // INITIALIZE SDK + DEVICE
//         // ============================
//         private void Initialize()
//         {
//             int ret = zkfp2.Init();
//             if (ret != zkfperrdef.ZKFP_ERR_OK)
//                 throw new Exception($"ZKFP Init failed: {ret}");

//             int deviceCount = zkfp2.GetDeviceCount();
//             if (deviceCount <= 0)
//                 throw new Exception("No Live10R device detected");

//             _deviceHandle = zkfp2.OpenDevice(0);
//             if (_deviceHandle == IntPtr.Zero)
//                 throw new Exception("Failed to open Live10R device");

//             _dbHandle = zkfp2.DBInit();
//             if (_dbHandle == IntPtr.Zero)
//                 throw new Exception("Failed to initialize fingerprint DB");

//             // Get image size (required for buffer)
//             byte[] paramValue = new byte[4];
//             int size = 4;
//             int width = 0, height = 0;

//             zkfp2.GetParameters(_deviceHandle, 1, paramValue, ref size);
//             zkfp2.ByteArray2Int(paramValue, ref width);

//             size = 4;
//             zkfp2.GetParameters(_deviceHandle, 2, paramValue, ref size);
//             zkfp2.ByteArray2Int(paramValue, ref height);

//             _fpBuffer = new byte[width * height];

//             _isInitialized = true;
//         }

//         // ============================
//         // ENROLL ONE FINGER
//         // ============================
//         // public void Enroll(int ownerId)
//         // {
//         //     EnsureReady();

//         //     int ret;
//         //     int templateLen = 2048;
//         //     byte[] template = new byte[2048];

//         //     // Wait until a finger is captured
//         //     while (true)
//         //     {
//         //         templateLen = 2048;
//         //         ret = zkfp2.AcquireFingerprint(
//         //             _deviceHandle,
//         //             _fpBuffer,
//         //             template,
//         //             ref templateLen
//         //         );

//         //         if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //             break;

//         //         Thread.Sleep(200);
//         //     }

//         //     ret = zkfp2.DBAdd(_dbHandle, ownerId, template);
//         //     if (ret != zkfperrdef.ZKFP_ERR_OK)
//         //         throw new Exception($"Enroll failed: {ret}");
//         // }

//         // public async Task EnrollAsync(int ownerId)
//         // {
//         //     EnsureReady();

//         //     await Task.Run(() =>
//         //     {
//         //         int ret;
//         //         int templateLen = 2048;
//         //         byte[] template = new byte[2048];

//         //         while (true)
//         //         {
//         //             templateLen = 2048;
//         //             ret = zkfp2.AcquireFingerprint(
//         //                 _deviceHandle,
//         //                 _fpBuffer,
//         //                 template,
//         //                 ref templateLen
//         //             );

//         //             if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //                 break;

//         //             Thread.Sleep(200);
//         //         }

//         //         ret = zkfp2.DBAdd(_dbHandle, ownerId, template);
//         //         if (ret != zkfperrdef.ZKFP_ERR_OK)
//         //             throw new Exception($"Enroll failed: {ret}");
//         //     });
//         // }

//         // public async Task<(int FingerprintId, byte[] Template)> EnrollAsync()
//         // {
//         //     EnsureReady();

//         //     return await Task.Run(() =>
//         //     {
//         //         int ret;
//         //         int templateLen = 2048;
//         //         byte[] template = new byte[2048];

//         //         // Wait for a valid fingerprint capture
//         //         while (true)
//         //         {
//         //             templateLen = 2048;
//         //             ret = zkfp2.AcquireFingerprint(
//         //                 _deviceHandle,
//         //                 _fpBuffer,
//         //                 template,
//         //                 ref templateLen
//         //             );

//         //             if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //                 break;

//         //             Thread.Sleep(200);
//         //         }

//         //         // Generate a new fingerprint ID
//         //         int fingerprintId = _nextFingerprintId++;

//         //         // Store inside SDK DB for fast matching
//         //         ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
//         //         if (ret != zkfperrdef.ZKFP_ERR_OK)
//         //             throw new Exception($"Enroll failed: {ret}");

//         //         // Trim template to actual length
//         //         byte[] finalTemplate = new byte[templateLen];
//         //         Array.Copy(template, finalTemplate, templateLen);

//         //         return (fingerprintId, finalTemplate);
//         //     });
//         // }

//         public async Task<byte[]> EnrollAsync()
//         {
//             EnsureReady();

//             return await Task.Run(() =>
//             {
//                 int ret;
//                 int templateLen = 2048;
//                 byte[] template = new byte[2048];

//                 while (true)
//                 {
//                     templateLen = 2048;
//                     ret = zkfp2.AcquireFingerprint(
//                         _deviceHandle,
//                         _fpBuffer,
//                         template,
//                         ref templateLen
//                     );

//                     if (ret == zkfperrdef.ZKFP_ERR_OK)
//                         break;

//                     Thread.Sleep(200);
//                 }

//                 byte[] finalTemplate = new byte[templateLen];
//                 Array.Copy(template, finalTemplate, templateLen);

//                 return finalTemplate;
//             });
//         }




//         // public async Task<int> EnrollAndReturnFingerprintIdAsync()
//         // {
//         //     EnsureReady();

//         //     int fingerprintId = _nextFingerprintId++;

//         //     await Task.Run(() =>
//         //     {
//         //         int ret;
//         //         int templateLen = 2048;
//         //         byte[] template = new byte[2048];

//         //         while (true)
//         //         {
//         //             templateLen = 2048;
//         //             ret = zkfp2.AcquireFingerprint(
//         //                 _deviceHandle,
//         //                 _fpBuffer,
//         //                 template,
//         //                 ref templateLen
//         //             );

//         //             if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //                 break;

//         //             Thread.Sleep(200);
//         //         }

//         //         ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
//         //         if (ret != zkfperrdef.ZKFP_ERR_OK)
//         //             throw new Exception($"Enroll failed: {ret}");
//         //     });

//         //     return fingerprintId;
//         // }


//         // ============================
//         // VERIFY FINGERPRINT
//         // ============================
//         // public int? Verify()
//         // {
//         //     EnsureReady();

//         //     int ret;
//         //     int fid = 0;
//         //     int score = 0;

//         //     while (true)
//         //     {
//         //         _capTemplateLen = 2048;
//         //         ret = zkfp2.AcquireFingerprint(
//         //             _deviceHandle,
//         //             _fpBuffer,
//         //             _capTemplate,
//         //             ref _capTemplateLen
//         //         );

//         //         if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //             break;

//         //         Thread.Sleep(200);
//         //     }

//         //     ret = zkfp2.DBIdentify(_dbHandle, _capTemplate, ref fid, ref score);
//         //     if (ret == zkfperrdef.ZKFP_ERR_OK)
//         //         return fid;

//         //     return null;
//         // }
//         public async Task<int?> VerifyAsync()
//         {
//             EnsureReady();

//             return await Task.Run<int?>(() =>
//             {
//                 int ret;
//                 int fid = 0;
//                 int score = 0;

//                 while (true)
//                 {
//                     _capTemplateLen = 2048;
//                     ret = zkfp2.AcquireFingerprint(
//                         _deviceHandle,
//                         _fpBuffer,
//                         _capTemplate,
//                         ref _capTemplateLen
//                     );

//                     if (ret == zkfperrdef.ZKFP_ERR_OK)
//                         break;

//                     Thread.Sleep(200);
//                 }

//                 ret = zkfp2.DBIdentify(_dbHandle, _capTemplate, ref fid, ref score);
//                 if (ret == zkfperrdef.ZKFP_ERR_OK)
//                     return fid;

//                 return null;
//             });
//         }

//         // ============================
//         // CLEANUP
//         // ============================
//         public void Dispose()
//         {
//             if (_deviceHandle != IntPtr.Zero)
//             {
//                 zkfp2.CloseDevice(_deviceHandle);
//                 _deviceHandle = IntPtr.Zero;
//             }

//             zkfp2.Terminate();
//         }

//         private void EnsureReady()
//         {
//             if (!_isInitialized)
//                 throw new Exception("FingerprintService not initialized");
//         }

//         public void UploadTemplate(int fingerprintId, byte[] template)
//         {
//             EnsureReady();

//             int ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
//             if (ret != zkfperrdef.ZKFP_ERR_OK)
//                 throw new Exception($"UploadTemplate failed: {ret}");
//         }

//     }
// }


// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using libzkfpcsharp;

// namespace BiometricStudentPickup.Services
// {
//     public class FingerprintService : IDisposable
//     {
//         private IntPtr _deviceHandle = IntPtr.Zero;
//         private IntPtr _dbHandle = IntPtr.Zero;

//         private byte[] _fpBuffer = Array.Empty<byte>();
//         private readonly byte[] _capTemplate = new byte[2048];
//         private int _capTemplateLen = 2048;

//         private bool _isInitialized = false;

//         // App-managed fingerprint IDs
//         private int _nextFingerprintId = 1;

//         public FingerprintService()
//         {
//             Initialize();
//         }

//         // ============================
//         // INITIALIZE SDK + DEVICE
//         // ============================
//         private void Initialize()
//         {
//             int ret = zkfp2.Init();
//             if (ret != zkfperrdef.ZKFP_ERR_OK)
//                 throw new Exception($"ZKFP Init failed: {ret}");

//             int deviceCount = zkfp2.GetDeviceCount();
//             if (deviceCount <= 0)
//                 throw new Exception("No fingerprint device detected");

//             _deviceHandle = zkfp2.OpenDevice(0);
//             if (_deviceHandle == IntPtr.Zero)
//                 throw new Exception("Failed to open fingerprint device");

//             _dbHandle = zkfp2.DBInit();
//             if (_dbHandle == IntPtr.Zero)
//                 throw new Exception("Failed to initialize fingerprint DB");

//             // Get image size
//             byte[] paramValue = new byte[4];
//             int size = 4;
//             int width = 0, height = 0;

//             zkfp2.GetParameters(_deviceHandle, 1, paramValue, ref size);
//             zkfp2.ByteArray2Int(paramValue, ref width);

//             size = 4;
//             zkfp2.GetParameters(_deviceHandle, 2, paramValue, ref size);
//             zkfp2.ByteArray2Int(paramValue, ref height);

//             _fpBuffer = new byte[width * height];

//             _isInitialized = true;
//         }

//         // ============================
//         // ENROLL FINGER (ID + TEMPLATE)
//         // ============================
//         public async Task<(int FingerprintId, byte[] Template)> EnrollAsync()
//         {
//             EnsureReady();

//             return await Task.Run(() =>
//             {
//                 int ret;
//                 int templateLen = 2048;
//                 byte[] template = new byte[2048];

//                 // Wait for valid fingerprint capture
//                 while (true)
//                 {
//                     templateLen = 2048;
//                     ret = zkfp2.AcquireFingerprint(
//                         _deviceHandle,
//                         _fpBuffer,
//                         template,
//                         ref templateLen
//                     );

//                     if (ret == zkfperrdef.ZKFP_ERR_OK)
//                         break;

//                     Thread.Sleep(200);
//                 }

//                 int fingerprintId = _nextFingerprintId++;

//                 // Store in SDK DB
//                 ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
//                 if (ret != zkfperrdef.ZKFP_ERR_OK)
//                     throw new Exception($"Enroll failed: {ret}");

//                 // Trim template
//                 byte[] finalTemplate = new byte[templateLen];
//                 Array.Copy(template, finalTemplate, templateLen);

//                 return (fingerprintId, finalTemplate);
//             });
//         }

//         // ============================
//         // VERIFY FINGERPRINT
//         // ============================
//         public async Task<int?> VerifyAsync()
//         {
//             EnsureReady();

//             return await Task.Run<int?>(() =>
//             {
//                 int ret;
//                 int fid = 0;
//                 int score = 0;

//                 while (true)
//                 {
//                     _capTemplateLen = 2048;
//                     ret = zkfp2.AcquireFingerprint(
//                         _deviceHandle,
//                         _fpBuffer,
//                         _capTemplate,
//                         ref _capTemplateLen
//                     );

//                     if (ret == zkfperrdef.ZKFP_ERR_OK)
//                         break;

//                     Thread.Sleep(200);
//                 }

//                 ret = zkfp2.DBIdentify(_dbHandle, _capTemplate, ref fid, ref score);
//                 if (ret == zkfperrdef.ZKFP_ERR_OK)
//                     return fid;

//                 return null;
//             });
//         }

//         // ============================
//         // RELOAD TEMPLATE INTO DEVICE
//         // ============================
//         public void UploadTemplate(int fingerprintId, byte[] template)
//         {
//             EnsureReady();

//             int ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
//             if (ret != zkfperrdef.ZKFP_ERR_OK)
//                 throw new Exception($"UploadTemplate failed: {ret}");

//             // Keep ID counter ahead
//             if (fingerprintId >= _nextFingerprintId)
//                 _nextFingerprintId = fingerprintId + 1;
//         }

//         // ============================
//         // CLEAR DEVICE DB (OPTIONAL)
//         // ============================
//         public void ClearDeviceDatabase()
//         {
//             EnsureReady();
//             zkfp2.DBClear(_dbHandle);
//         }

//         // ============================
//         // CLEANUP
//         // ============================
//         public void Dispose()
//         {
//             if (_deviceHandle != IntPtr.Zero)
//             {
//                 zkfp2.CloseDevice(_deviceHandle);
//                 _deviceHandle = IntPtr.Zero;
//             }

//             zkfp2.Terminate();
//         }

//         private void EnsureReady()
//         {
//             if (!_isInitialized)
//                 throw new Exception("FingerprintService not initialized");
//         }
//     }
// }


using System;
using System.Threading;
using System.Threading.Tasks;
using libzkfpcsharp;

namespace BiometricStudentPickup.Services
{
    public class FingerprintService : IDisposable
    {
        private IntPtr _deviceHandle = IntPtr.Zero;
        private IntPtr _dbHandle = IntPtr.Zero;

        private byte[] _fpBuffer = Array.Empty<byte>();
        private readonly byte[] _capTemplate = new byte[2048];
        private int _capTemplateLen = 2048;

        private bool _isInitialized = false;

        public FingerprintService()
        {
            Initialize();
        }

        // ============================
        // INITIALIZE SDK + DEVICE
        // ============================
        private void Initialize()
        {
            int ret = zkfp2.Init();
            if (ret != zkfperrdef.ZKFP_ERR_OK)
                throw new Exception($"ZKFP Init failed: {ret}");

            int deviceCount = zkfp2.GetDeviceCount();
            if (deviceCount <= 0)
                throw new Exception("No fingerprint device detected");

            _deviceHandle = zkfp2.OpenDevice(0);
            if (_deviceHandle == IntPtr.Zero)
                throw new Exception("Failed to open fingerprint device");

            _dbHandle = zkfp2.DBInit();
            if (_dbHandle == IntPtr.Zero)
                throw new Exception("Failed to initialize fingerprint DB");

            // Get image size
            byte[] paramValue = new byte[4];
            int size = 4;
            int width = 0, height = 0;

            zkfp2.GetParameters(_deviceHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref width);

            size = 4;
            zkfp2.GetParameters(_deviceHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref height);

            _fpBuffer = new byte[width * height];

            _isInitialized = true;
        }

        // ============================
        // ENROLL FINGER (TEMPLATE ONLY)
        // ============================
        public async Task<byte[]> EnrollAsync()
        {
            EnsureReady();

            return await Task.Run(() =>
            {
                int ret;
                int templateLen = 2048;
                byte[] template = new byte[2048];

                while (true)
                {
                    templateLen = 2048;
                    ret = zkfp2.AcquireFingerprint(
                        _deviceHandle,
                        _fpBuffer,
                        template,
                        ref templateLen
                    );

                    if (ret == zkfperrdef.ZKFP_ERR_OK)
                        break;

                    Thread.Sleep(200);
                }

                byte[] finalTemplate = new byte[templateLen];
                Array.Copy(template, finalTemplate, templateLen);

                return finalTemplate;
            });
        }

        // ============================
        // VERIFY FINGERPRINT
        // ============================
        public async Task<int?> VerifyAsync()
        {
            EnsureReady();

            return await Task.Run<int?>(() =>
            {
                int ret;
                int fid = 0;
                int score = 0;

                while (true)
                {
                    _capTemplateLen = 2048;
                    ret = zkfp2.AcquireFingerprint(
                        _deviceHandle,
                        _fpBuffer,
                        _capTemplate,
                        ref _capTemplateLen
                    );

                    if (ret == zkfperrdef.ZKFP_ERR_OK)
                        break;

                    Thread.Sleep(200);
                }

                ret = zkfp2.DBIdentify(_dbHandle, _capTemplate, ref fid, ref score);
                if (ret == zkfperrdef.ZKFP_ERR_OK)
                    return fid;

                return null;
            });
        }

        // ============================
        // RELOAD TEMPLATE INTO DEVICE
        // ============================
        public void UploadTemplate(int fingerprintId, byte[] template)
        {
            EnsureReady();

            int ret = zkfp2.DBAdd(_dbHandle, fingerprintId, template);
            if (ret != zkfperrdef.ZKFP_ERR_OK)
                throw new Exception($"UploadTemplate failed: {ret}");
        }

        // ============================
        // CLEAR DEVICE DB
        // ============================
        public void ClearDeviceDatabase()
        {
            EnsureReady();
            zkfp2.DBClear(_dbHandle);
        }

        // ============================
        // CLEANUP
        // ============================
        public void Dispose()
        {
            if (_deviceHandle != IntPtr.Zero)
            {
                zkfp2.CloseDevice(_deviceHandle);
                _deviceHandle = IntPtr.Zero;
            }

            zkfp2.Terminate();
        }

        private void EnsureReady()
        {
            if (!_isInitialized)
                throw new Exception("FingerprintService not initialized");
        }
    }
}
