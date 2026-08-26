#![allow(unused)]

extern crate alloc;
use alloc::vec::Vec;
use core::slice;
use umbral_pre::*;
use umbral_pre::{DefaultDeserialize, DefaultSerialize};

fn err(rc: i32, out_ptr: *mut *mut u8, out_len: *mut i32) -> i32 {
    unsafe {
        if !out_ptr.is_null() { *out_ptr = core::ptr::null_mut(); }
        if !out_len.is_null() { *out_len = 0; }
    }
    rc
}

#[no_mangle]
pub extern "C" fn umbral_encapsulate_with_tag(
    pk_author: *const u8, pk_author_len: i32,
    tag_utf8: *const u8, tag_len: i32,
    key: *const u8, key_len: i32,
    out_ptr: *mut *mut u8, out_len: *mut i32,
) -> i32 {
    if pk_author.is_null() || tag_utf8.is_null() || key.is_null() { return err(2, out_ptr, out_len); }
    let pk_bytes = unsafe { slice::from_raw_parts(pk_author, pk_author_len as usize) };
    let _tag_bytes = unsafe { slice::from_raw_parts(tag_utf8, tag_len as usize) };
    let key_bytes = unsafe { slice::from_raw_parts(key, key_len as usize) };

    let alice_pk = match PublicKey::try_from_compressed_bytes(pk_bytes) { Ok(v) => v, Err(_) => return err(3, out_ptr, out_len) };
    let (capsule, ciphertext) = match encrypt(&alice_pk, key_bytes) { Ok(t) => t, Err(_) => return err(4, out_ptr, out_len) };

    let cap = match capsule.to_bytes() { Ok(v) => v, Err(_) => return err(10, out_ptr, out_len) };
    let mut blob = Vec::with_capacity(4 + cap.len() + 4 + ciphertext.len() + 4 + pk_bytes.len());
    let clen = (cap.len() as u32).to_le_bytes();
    blob.extend_from_slice(&clen);
    blob.extend_from_slice(&cap);
    let ctlen = (ciphertext.len() as u32).to_le_bytes();
    blob.extend_from_slice(&ctlen);
    blob.extend_from_slice(&ciphertext);
    let pklen = (pk_bytes.len() as u32).to_le_bytes();
    blob.extend_from_slice(&pklen);
    blob.extend_from_slice(pk_bytes);

    unsafe {
        let p = libc::malloc(blob.len()) as *mut u8;
        if p.is_null() { return err(5, out_ptr, out_len); }
        core::ptr::copy_nonoverlapping(blob.as_ptr(), p, blob.len());
        *out_ptr = p;
        *out_len = blob.len() as i32;
    }
    0
}

#[no_mangle]
pub extern "C" fn umbral_generate_rekey(
    sk_author: *const u8, sk_author_len: i32,
    pk_recipient: *const u8, pk_recipient_len: i32,
    _tag_utf8: *const u8, _tag_len: i32,
    out_ptr: *mut *mut u8, out_len: *mut i32,
) -> i32 {
    if sk_author.is_null() || pk_recipient.is_null() { return err(2, out_ptr, out_len); }
    let sk_bytes = unsafe { slice::from_raw_parts(sk_author, sk_author_len as usize) };
    let pk_bytes = unsafe { slice::from_raw_parts(pk_recipient, pk_recipient_len as usize) };

    let alice_sk = match SecretKey::try_from_be_bytes(sk_bytes) { Ok(v) => v, Err(_) => return err(3, out_ptr, out_len) };
    let bob_pk = match PublicKey::try_from_compressed_bytes(pk_bytes) { Ok(v) => v, Err(_) => return err(4, out_ptr, out_len) };

    let signer = Signer::new(SecretKey::random());
    let verifying_pk = signer.verifying_key();
    let vkfrags = generate_kfrags(&alice_sk, &bob_pk, &signer, 1, 1, true, true);
    let kfrag_unverified = vkfrags[0].clone().unverify();
    let kfrag_ser = match <KeyFrag as DefaultSerialize>::to_bytes(&kfrag_unverified) { Ok(v) => v, Err(_) => return err(10, out_ptr, out_len) };
    let vpk_bytes = verifying_pk.to_compressed_bytes();
    let pkb_bytes = bob_pk.to_compressed_bytes();
    let mut blob = Vec::with_capacity(4 + vpk_bytes.len() + 4 + pkb_bytes.len() + 4 + kfrag_ser.len());
    let vlen = (vpk_bytes.len() as u32).to_le_bytes();
    blob.extend_from_slice(&vlen);
    blob.extend_from_slice(&vpk_bytes);
    let pkblen = (pkb_bytes.len() as u32).to_le_bytes();
    blob.extend_from_slice(&pkblen);
    blob.extend_from_slice(&pkb_bytes);
    let klen = (kfrag_ser.len() as u32).to_le_bytes();
    blob.extend_from_slice(&klen);
    blob.extend_from_slice(&kfrag_ser);
    unsafe {
        let p = libc::malloc(blob.len()) as *mut u8;
        if p.is_null() { return err(5, out_ptr, out_len); }
        core::ptr::copy_nonoverlapping(blob.as_ptr(), p, blob.len());
        *out_ptr = p;
        *out_len = blob.len() as i32;
    }
    0
}

#[no_mangle]
pub extern "C" fn umbral_reencrypt(
    capsule_ptr: *const u8, capsule_len: i32,
    rk_ptr: *const u8, rk_len: i32,
    out_ptr: *mut *mut u8, out_len: *mut i32,
) -> i32 {
    if capsule_ptr.is_null() || rk_ptr.is_null() { return err(2, out_ptr, out_len); }
    let blob = unsafe { slice::from_raw_parts(capsule_ptr, capsule_len as usize) };
    let rk_blob = unsafe { slice::from_raw_parts(rk_ptr, rk_len as usize) };

    if blob.len() < 4 { return err(6, out_ptr, out_len); }
    let mut off = 0usize;
    let cap_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + cap_len { return err(6, out_ptr, out_len); }
    let cap_ser = &blob[off..off+cap_len]; off += cap_len;
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let ct_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + ct_len { return err(6, out_ptr, out_len); }
    let _ct = &blob[off..off+ct_len]; off += ct_len;
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let pk_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + pk_len { return err(6, out_ptr, out_len); }
    let pk_a_bytes = &blob[off..off+pk_len]; off += pk_len;

    let capsule = match <Capsule as DefaultDeserialize>::from_bytes(cap_ser) { Ok(v) => v, Err(_) => return err(7, out_ptr, out_len) };

    if rk_blob.len() < 12 { return err(6, out_ptr, out_len); }
    let mut rk_off = 0usize;
    let vpk_len = u32::from_le_bytes(rk_blob[rk_off..rk_off+4].try_into().unwrap()) as usize; rk_off += 4;
    if rk_blob.len() < rk_off + vpk_len { return err(6, out_ptr, out_len); }
    let vpk_bytes = &rk_blob[rk_off..rk_off+vpk_len]; rk_off += vpk_len;
    if rk_blob.len() < rk_off + 4 { return err(6, out_ptr, out_len); }
    let pkb_len = u32::from_le_bytes(rk_blob[rk_off..rk_off+4].try_into().unwrap()) as usize; rk_off += 4;
    if rk_blob.len() < rk_off + pkb_len { return err(6, out_ptr, out_len); }
    let pkb_bytes = &rk_blob[rk_off..rk_off+pkb_len]; rk_off += pkb_len;
    if rk_blob.len() < rk_off + 4 { return err(6, out_ptr, out_len); }
    let kf_len = u32::from_le_bytes(rk_blob[rk_off..rk_off+4].try_into().unwrap()) as usize; rk_off += 4;
    if rk_blob.len() < rk_off + kf_len { return err(6, out_ptr, out_len); }
    let kfrag_bytes = &rk_blob[rk_off..rk_off+kf_len];

    let verifying_pk = match PublicKey::try_from_compressed_bytes(vpk_bytes) { Ok(v) => v, Err(_) => return err(8, out_ptr, out_len) };
    let bob_pk = match PublicKey::try_from_compressed_bytes(pkb_bytes) { Ok(v) => v, Err(_) => return err(8, out_ptr, out_len) };
    let kfrag_unverified: KeyFrag = match <KeyFrag as DefaultDeserialize>::from_bytes(kfrag_bytes) { Ok(v) => v, Err(_) => return err(8, out_ptr, out_len) };
    let author_pk = match PublicKey::try_from_compressed_bytes(pk_a_bytes) { Ok(v) => v, Err(_) => return err(8, out_ptr, out_len) };
    let verified_kfrag = match kfrag_unverified.verify(&verifying_pk, Some(&author_pk), Some(&bob_pk)) { Ok(v) => v, Err(_) => return err(9, out_ptr, out_len) };
    let vcfrag = reencrypt(&capsule, verified_kfrag);
    let cfrag_unverified = vcfrag.clone().unverify();
    let cfrag_ser = match <CapsuleFrag as DefaultSerialize>::to_bytes(&cfrag_unverified) { Ok(v) => v, Err(_) => return err(10, out_ptr, out_len) };

    let mut out = Vec::with_capacity(blob.len() + 4 + vpk_bytes.len() + 4 + cfrag_ser.len());
    out.extend_from_slice(&blob);
    let vlen = (vpk_bytes.len() as u32).to_le_bytes();
    out.extend_from_slice(&vlen);
    out.extend_from_slice(vpk_bytes);
    let cflen = (cfrag_ser.len() as u32).to_le_bytes();
    out.extend_from_slice(&cflen);
    out.extend_from_slice(&cfrag_ser);

    unsafe {
        let p = libc::malloc(out.len()) as *mut u8;
        if p.is_null() { return err(5, out_ptr, out_len); }
        core::ptr::copy_nonoverlapping(out.as_ptr(), p, out.len());
        *out_ptr = p;
        *out_len = out.len() as i32;
    }
    0
}

#[no_mangle]
pub extern "C" fn umbral_decapsulate_for_recipient(
    capsule_ptr: *const u8, capsule_len: i32,
    sk_recipient: *const u8, sk_recipient_len: i32,
    _tag_utf8: *const u8, _tag_len: i32,
    out_ptr: *mut *mut u8, out_len: *mut i32,
) -> i32 {
    if capsule_ptr.is_null() || sk_recipient.is_null() { return err(2, out_ptr, out_len); }
    let blob = unsafe { slice::from_raw_parts(capsule_ptr, capsule_len as usize) };
    let sk_bytes = unsafe { slice::from_raw_parts(sk_recipient, sk_recipient_len as usize) };
    let bob_sk = match SecretKey::try_from_be_bytes(sk_bytes) { Ok(v) => v, Err(_) => return err(3, out_ptr, out_len) };

    if blob.len() < 4 { return err(6, out_ptr, out_len); }
    let mut off = 0usize;
    let cap_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + cap_len { return err(6, out_ptr, out_len); }
    let cap_ser = &blob[off..off+cap_len]; off += cap_len;
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let ct_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + ct_len { return err(6, out_ptr, out_len); }
    let ct = &blob[off..off+ct_len]; off += ct_len;
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let pk_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + pk_len { return err(6, out_ptr, out_len); }
    let pk_a_bytes = &blob[off..off+pk_len]; off += pk_len;
    let capsule = match <Capsule as DefaultDeserialize>::from_bytes(cap_ser) { Ok(v) => v, Err(_) => return err(7, out_ptr, out_len) };
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let vpk_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + vpk_len { return err(6, out_ptr, out_len); }
    let vpk_bytes = &blob[off..off+vpk_len]; off += vpk_len;
    if blob.len() < off + 4 { return err(6, out_ptr, out_len); }
    let cfrag_len = u32::from_le_bytes(blob[off..off+4].try_into().unwrap()) as usize; off += 4;
    if blob.len() < off + cfrag_len { return err(6, out_ptr, out_len); }
    let cfrag_ser = &blob[off..off+cfrag_len];

    let verifying_pk = match PublicKey::try_from_compressed_bytes(vpk_bytes) { Ok(v) => v, Err(_) => return err(8, out_ptr, out_len) };
    let cfrag_unverified: CapsuleFrag = match <CapsuleFrag as DefaultDeserialize>::from_bytes(cfrag_ser) { Ok(v) => v, Err(_) => return err(7, out_ptr, out_len) };
    let alice_pk = match PublicKey::try_from_compressed_bytes(pk_a_bytes) { Ok(v) => v, Err(_) => return err(3, out_ptr, out_len) };
    let bob_pk = bob_sk.public_key();
    let verified_cfrag = match cfrag_unverified.verify(&capsule, &verifying_pk, &alice_pk, &bob_pk) { Ok(v) => v, Err(_) => return err(9, out_ptr, out_len) };

    let key = match decrypt_reencrypted(&bob_sk, &alice_pk, &capsule, [verified_cfrag], ct) { Ok(pt) => pt, Err(_) => return err(11, out_ptr, out_len) };
    let key_bytes: &[u8] = &key;
    unsafe {
        let p = libc::malloc(key_bytes.len()) as *mut u8;
        if p.is_null() { return err(5, out_ptr, out_len); }
        core::ptr::copy_nonoverlapping(key_bytes.as_ptr(), p, key_bytes.len());
        *out_ptr = p;
        *out_len = key_bytes.len() as i32;
    }
    0
}

#[no_mangle]
pub extern "C" fn umbral_free(buffer: *mut u8) {
    if !buffer.is_null() {
        unsafe { libc::free(buffer as *mut core::ffi::c_void); }
    }
}


