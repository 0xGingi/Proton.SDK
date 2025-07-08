#ifndef PROTON_SDK_H
#define PROTON_SDK_H

#include <stdint.h>
#include <stdbool.h>

// Structs 

typedef struct {
    const uint8_t* pointer;
    size_t length;
} ByteArray;

// Callbacks

typedef struct {
    const void* state;
    void (*on_success)(const void*, ByteArray);
    void (*on_failure)(const void*, ByteArray); 
    intptr_t cancellation_token_source_handle;
} AsyncCallback;

typedef struct {
    const void* state;
    void (*callback)(const void*, ByteArray);
} Callback;

typedef struct {
    AsyncCallback async_callback;
    Callback progress_callback;
} AsyncCallbackWithProgress;

typedef struct {
    const void* state;
    bool (*callback)(const void*, ByteArray); // KeyCacheMissMessage
} BooleanCallback;

// Cancellation

intptr_t cancellation_token_source_create();

void cancellation_token_source_cancel(
    intptr_t cancellation_token_source_handle
);

void cancellation_token_source_free(
    intptr_t cancellation_token_source_handle
);

// Sessions

int session_begin(
    intptr_t unused_handle, // Added for the sake of uniformity
    ByteArray pointer, // SessionBeginRequest
    Callback request_response_body_callback,
    BooleanCallback secret_requested_callback,
    Callback tokens_refreshed_callback,
    AsyncCallback callback
);

int session_resume(
    ByteArray pointer, // SessionResumeRequest
    Callback request_response_body_callback,
    BooleanCallback secret_requested_callback,
    Callback tokens_refreshed_callback,
    intptr_t* session_handle // TODO: SessionResumeResponse
);

int session_renew(
    intptr_t old_session_handle,
    ByteArray pointer, // SessionRenewRequest
    Callback tokens_refreshed_callback,
    intptr_t* new_session_handle // TODO: SessionRenewResponse
);

int session_end(
    intptr_t session_handle, // Todo: SessionEndRequest
    AsyncCallback callback
);

void session_free(intptr_t session_handle);

// Keys

int session_register_armored_locked_user_key(
    intptr_t session_handle,
    ByteArray armoredUserKey
);

int session_register_address_keys(
    intptr_t session_handle,
    ByteArray pointer // AddressKeyRegistrationRequest
);

// Nodes

int node_decrypt_armored_name(
    intptr_t client_handle,
    ByteArray pointer,
    AsyncCallback callback
);

// Drive client 

int drive_client_create(
    intptr_t session_handle,
    intptr_t observability_handle,
    ByteArray pointer, // ProtonDriveClientCreateRequest
    intptr_t* out_client_handle
);

int drive_client_register_node_keys(
    intptr_t client_handle,
    ByteArray pointer // NodeKeysRegistrationRequest
);

int drive_client_register_share_key(
    intptr_t client_handle,
    ByteArray pointer // ShareKeyRegistrationRequest
);

ByteArray drive_client_get_volumes(
    intptr_t client_handle,
    intptr_t cancellation_token_source_handle
);

ByteArray drive_client_get_shares(
    intptr_t client_handle,
    ByteArray volume_metadata,
    intptr_t cancellation_token
);

void drive_client_free(intptr_t client_handle);

// Observability service

int observability_service_start_new(
    intptr_t session_handle,
    intptr_t* out_observability_handle
);

int observability_service_flush(
    intptr_t observability_handle,
    AsyncCallback callback
);

int observability_service_free(intptr_t observability_handle);

// Downloads

int downloader_create(
    intptr_t client_handle,
    ByteArray pointer, // Empty
    AsyncCallback callback
);

// Response: File
int downloader_download_file(
    intptr_t downloader_handle,
    ByteArray pointer, // FileDownloadRequest
    AsyncCallbackWithProgress callback
);

void downloader_free(intptr_t downloader_handle);

// Uploads

// Response: IntResponse
int uploader_create(
    intptr_t client_handle,
    ByteArray pointer, // FileUploaderCreationRequest
    AsyncCallback callback
);

// Response: FileNode
int uploader_upload_file_or_revision(
    intptr_t uploader_handle,
    ByteArray pointer, // FileUploadRequest
    AsyncCallbackWithProgress callback
);

// Response: Revision
int uploader_upload_revision(
    intptr_t uploader_handle,
    ByteArray pointer, // RevisionUploadRequest
    AsyncCallbackWithProgress callback
);

void uploader_free(intptr_t uploader_handle);

// Logger

int logger_provider_create(
    Callback log_callback,
    intptr_t* logger_provider_handle
);

#endif PROTON_SDK_H
