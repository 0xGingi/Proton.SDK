#ifndef PROTON_SDK_H
#define PROTON_SDK_H

#include <stdint.h>
#include <stdbool.h>

typedef struct {
    const uint8_t* pointer;
    size_t length;
} ByteArray;

typedef struct {
    const char* pointer;
    size_t length;
} Utf8String;

typedef struct {
    int code;
    Utf8String message;
} SdkError;

typedef struct {
    const void* state;
    void (*on_success)(const void*, intptr_t);
    void (*on_failure)(const void*, SdkError);
    intptr_t cancellation_token_source_handle;
} AsyncHandleCallback;

typedef struct {
    const void* state;
    void (*on_success)(const void*, Utf8String);
    void (*on_failure)(const void*, SdkError);
    intptr_t cancellation_token_source_handle;
} AsyncUtf8StringCallback;

typedef struct {
    const void* state;
    void (*on_success)(const void*);
    void (*on_failure)(const void*, SdkError);
} AsyncVoidCallbackWithoutCancellation;

typedef struct {
    Utf8String app_version;
    Utf8String user_agent;
    Utf8String base_url;
    bool ignore_ssl_certificate_errors;
    intptr_t logger_provider_handle;
} ProtonClientOptions;

int session_begin(
    Utf8String username,
    Utf8String password,
    ProtonClientOptions options,
    AsyncHandleCallback callback);

int session_resume(
    Utf8String id,
    Utf8String username,
    Utf8String userId,
    Utf8String accessToken,
    Utf8String refreshToken,
    Utf8String scopes,
    bool isWaitingForSecondFactorCode,
    uint8_t passwordMode,
    ProtonClientOptions options,
    intptr_t* session_handle
);

int session_add_user_key(
    intptr_t session_handle,
    Utf8String keyId,
    ByteArray keyData);

int session_add_armored_locked_user_key(
    intptr_t session_handle,
    Utf8String keyId,
    ByteArray keyData,
    Utf8String passphrase
);

int session_end(
    intptr_t session_handle,
    AsyncVoidCallbackWithoutCancellation callback
);

void session_free(intptr_t session_handle);

typedef struct {
    Utf8String id;
    Utf8String membership_address_id;
    Utf8String membership_email_address;
} ShareForCommand;

typedef struct {
    Utf8String volume_id;
    Utf8String id;
} NodeIdentity;

typedef struct {
    Utf8String volume_id;
    Utf8String id;
    Utf8String parent_id;
    Utf8String name;
    uint8_t state;
    ByteArray name_hash_digest;
} FileNode;

typedef struct {
    Utf8String volume_id;
    Utf8String file_id;
    Utf8String id;
    uint8_t state;
    int64_t size;
    int64_t quota_consumption;
    int64_t creation_time;
    ByteArray manifest_signature;
    Utf8String signature_email_address;
    Utf8String samples_sha256_digests;
} Revision;

typedef struct {
    FileNode file;
    Revision revision;
} FileRevisionPair;

typedef struct {
    Utf8String id;
    uint8_t state;
    ByteArray manifest_signature;
    Utf8String signature_email_address;
    Utf8String samples_sha256_digests;
} RevisionForTransfer;

typedef struct {
    const void* state;
    void (*on_success)(const void*, FileRevisionPair);
    void (*on_failure)(const void*, SdkError);
    intptr_t cancellation_token_source_handle;
} AsyncFileRevisionPairCallback;

int drive_client_create(
    intptr_t session_handle,
    intptr_t* out_client_handle
);

int drive_client_create_file(
    intptr_t client_handle,
    ShareForCommand share,
    NodeIdentity parent_folder,
    Utf8String name,
    Utf8String media_type,
    AsyncFileRevisionPairCallback callback
);

int drive_client_open_revision_for_reading(
    intptr_t client_handle,
    Utf8String share_id,
    NodeIdentity file,
    RevisionForTransfer revision,
    AsyncHandleCallback callback
);

int drive_client_open_revision_for_writing(
    intptr_t client_handle,
    ShareForCommand share,
    NodeIdentity file,
    RevisionForTransfer revision,
    AsyncHandleCallback callback
);

int drive_client_free(intptr_t client_handle);

typedef struct {
    const void* state;
    void (*on_success)(const void*, uint8_t);
    void (*on_failure)(const void*, SdkError);
    intptr_t cancellation_token_source_handle;
} AsyncUInt8Callback;

typedef struct {
    intptr_t continuation_handle;
    void (*on_success)(intptr_t);
    void (*on_failure)(intptr_t);
} ExternalAsyncCallback;

typedef struct {
    const void* state;
    void (*write)(const void*, const uint8_t*, size_t, ExternalAsyncCallback);
} ExternalWriter;

int revision_reader_read(
    intptr_t reader_handle,
    ExternalWriter writer,
    AsyncUInt8Callback callback
);

int revision_reader_read_to_path(
    intptr_t reader_handle,
    Utf8String target_file_path,
    AsyncUInt8Callback callback
);

int revision_reader_free(intptr_t reader_handle);

int revision_writer_write_to_path(
    intptr_t writerHandle,
    Utf8String targetFilePath,
    long lastModificationTime,
    AsyncUInt8Callback callback
);

int node_decrypt_armored_name(
    intptr_t client_handle,
    Utf8String share_id,
    Utf8String volume_id,
    Utf8String parent_link_id,
    Utf8String armored_encrypted_name,
    AsyncUtf8StringCallback callback
);

typedef struct {
    uint8_t level;
    Utf8String message;
    Utf8String categoryName;
} LogEvent;

typedef struct {
    const void* state;
    void (*log_callback)(const void*, LogEvent);
} LogCallback;

int logger_provider_create(
    LogCallback log_callback,
    intptr_t* logger_provider_handle
);

#endif PROTON_SDK_H