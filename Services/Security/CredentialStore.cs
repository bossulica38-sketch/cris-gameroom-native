// CRIS-GAMEROOM-CREDENTIAL-STORE-V1
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace CrisGameRoom.Services.Security;

public static class CredentialStore
{
    private const string Target = "CRIS.GameRoom.Native.Login";
    private const uint GenericCredential = 1;
    private const uint PersistLocalMachine = 2;
    private const uint MaxCredentialBlobSize = 2560;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
        [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
        public NativeFileTime LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
        [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
    }

    private sealed class SavedCredentials
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWriteW(ref NativeCredential userCredential, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDeleteW(string target, uint type, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr buffer);

    public static bool TryLoad(out string userName, out string password)
    {
        userName = "";
        password = "";

        if (!OperatingSystem.IsWindows())
            return false;

        IntPtr pointer = IntPtr.Zero;
        try
        {
            if (!CredReadW(Target, GenericCredential, 0, out pointer) || pointer == IntPtr.Zero)
                return false;

            NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(pointer);
            if (credential.CredentialBlob == IntPtr.Zero ||
                credential.CredentialBlobSize == 0 ||
                credential.CredentialBlobSize > MaxCredentialBlobSize)
                return false;

            byte[] bytes = new byte[checked((int)credential.CredentialBlobSize)];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);

            SavedCredentials? saved = JsonSerializer.Deserialize<SavedCredentials>(
                Encoding.UTF8.GetString(bytes));

            if (saved is null ||
                string.IsNullOrWhiteSpace(saved.UserName) ||
                string.IsNullOrEmpty(saved.Password))
                return false;

            userName = saved.UserName;
            password = saved.Password;
            return true;
        }
        catch
        {
            userName = "";
            password = "";
            return false;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
                CredFree(pointer);
        }
    }

    public static void Save(string userName, string password)
    {
        if (!OperatingSystem.IsWindows() ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrEmpty(password))
            return;

        IntPtr blobPointer = IntPtr.Zero;
        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
                new SavedCredentials { UserName = userName, Password = password });

            if (bytes.Length == 0 || bytes.Length > MaxCredentialBlobSize)
                return;

            blobPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, blobPointer, bytes.Length);

            var credential = new NativeCredential
            {
                Flags = 0,
                Type = GenericCredential,
                TargetName = Target,
                Comment = "CRIS GameRoom sign-in",
                LastWritten = new NativeFileTime(),
                CredentialBlobSize = (uint)bytes.Length,
                CredentialBlob = blobPointer,
                Persist = PersistLocalMachine,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = "",
                UserName = userName
            };

            _ = CredWriteW(ref credential, 0);
        }
        catch
        {
            // Stocarea parolei nu trebuie să blocheze autentificarea.
        }
        finally
        {
            if (blobPointer != IntPtr.Zero)
                Marshal.FreeHGlobal(blobPointer);
        }
    }

    public static void Delete()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            _ = CredDeleteW(Target, GenericCredential, 0);
        }
        catch
        {
        }
    }
}
