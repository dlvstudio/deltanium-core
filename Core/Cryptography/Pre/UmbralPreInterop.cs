using System;
using System.Runtime.InteropServices;

namespace Deltanium.Core.Cryptography.Pre
{
    /// <summary>
    /// Native interop for Umbral PRE (via a C ABI shim built on rust-umbral).
    /// Expected dynamic library name: "umbral_pre" (libumbral_pre.so/.dylib/umbral_pre.dll).
    /// Functions must be provided by the Rust shim wrapping https://github.com/nucypher/rust-umbral.
    /// </summary>
    public static class UmbralPre
    {
        private const string Lib = "umbral_pre";

        [DllImport(Lib, EntryPoint = "umbral_encapsulate_with_tag", CallingConvention = CallingConvention.Cdecl)]
        private static extern int umbral_encapsulate_with_tag(
            byte[] pk_author, int pk_author_len,
            byte[] tag_utf8, int tag_len,
            byte[] key, int key_len,
            out IntPtr capsule_ptr, out int capsule_len);

        [DllImport(Lib, EntryPoint = "umbral_generate_rekey", CallingConvention = CallingConvention.Cdecl)]
        private static extern int umbral_generate_rekey(
            byte[] sk_author, int sk_author_len,
            byte[] pk_recipient, int pk_recipient_len,
            byte[] tag_utf8, int tag_len,
            out IntPtr rk_ptr, out int rk_len);

        [DllImport(Lib, EntryPoint = "umbral_reencrypt", CallingConvention = CallingConvention.Cdecl)]
        private static extern int umbral_reencrypt(
            byte[] capsule_ptr, int capsule_len,
            byte[] rk_ptr, int rk_len,
            out IntPtr out_capsule_ptr, out int out_capsule_len);

        [DllImport(Lib, EntryPoint = "umbral_decapsulate_for_recipient", CallingConvention = CallingConvention.Cdecl)]
        private static extern int umbral_decapsulate_for_recipient(
            byte[] capsule_ptr, int capsule_len,
            byte[] sk_recipient, int sk_recipient_len,
            byte[] tag_utf8, int tag_len,
            out IntPtr key_ptr, out int key_len);

        [DllImport(Lib, EntryPoint = "umbral_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void umbral_free(IntPtr buffer);

        public static byte[] EncapsulateWithTag(byte[] pkAuthor, string tag, byte[] key)
        {
            var tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
            var rc = umbral_encapsulate_with_tag(pkAuthor, pkAuthor?.Length ?? 0, tagBytes, tagBytes.Length, key, key?.Length ?? 0, out var ptr, out var len);
            if (rc != 0) throw new InvalidOperationException($"umbral_encapsulate_with_tag failed (rc={rc})");
            try { return CopyAndFree(ptr, len); } finally { /* ptr freed in CopyAndFree */ }
        }

        public static byte[] GenerateRekey(byte[] skAuthor, byte[] pkRecipient, string tag)
        {
            var tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
            var rc = umbral_generate_rekey(skAuthor, skAuthor?.Length ?? 0, pkRecipient, pkRecipient?.Length ?? 0, tagBytes, tagBytes.Length, out var ptr, out var len);
            if (rc != 0) throw new InvalidOperationException($"umbral_generate_rekey failed (rc={rc})");
            try { return CopyAndFree(ptr, len); } finally { /* ptr freed in CopyAndFree */ }
        }

        public static byte[] Reencrypt(byte[] capsuleForAuthor, byte[] rekey)
        {
            var rc = umbral_reencrypt(capsuleForAuthor, capsuleForAuthor?.Length ?? 0, rekey, rekey?.Length ?? 0, out var ptr, out var len);
            if (rc != 0) throw new InvalidOperationException($"umbral_reencrypt failed (rc={rc})");
            try { return CopyAndFree(ptr, len); } finally { /* ptr freed in CopyAndFree */ }
        }

        public static byte[] DecapsulateForRecipient(byte[] capsuleForRecipient, byte[] skRecipient, string tag)
        {
            var tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
            var rc = umbral_decapsulate_for_recipient(capsuleForRecipient, capsuleForRecipient?.Length ?? 0, skRecipient, skRecipient?.Length ?? 0, tagBytes, tagBytes.Length, out var ptr, out var len);
            if (rc != 0) throw new InvalidOperationException($"umbral_decapsulate_for_recipient failed (rc={rc})");
            try { return CopyAndFree(ptr, len); } finally { /* ptr freed in CopyAndFree */ }
        }

        private static byte[] CopyAndFree(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero || len <= 0) return Array.Empty<byte>();
            var managed = new byte[len];
            Marshal.Copy(ptr, managed, 0, len);
            umbral_free(ptr);
            return managed;
        }
    }
}


