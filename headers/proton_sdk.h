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
    void (*on_progress)(ByteArray);
    void (*on_success)(const void*, ByteArray);
    void (*on_failure)(const void*, ByteArray); 
    intptr_t cancellation_token_source_handle;
} AsyncCallbackWithProgress;

typedef struct {
    const void* state;
    void (*callback)(const void*, ByteArray);
} Callback;

// Sessions

int session_begin(
    intptr_t unused_handle, // Added for the sake of uniformity
    ByteArray pointer, // SessionBeginRequest
    AsyncCallback callback
);

int session_resume(
    ByteArray pointer, // SessionResumeRequest
    intptr_t* session_handle // TODO: SessionResumeResponse
);

int session_renew(
    ByteArray pointer, // SessionRenewRequest
    intptr_t* new_session_handle // TODO: SessionRenewResponse
);

int session_end(
    intptr_t session_handle, // Todo: SessionEndRequest
    AsyncCallback callback
);

void session_free(intptr_t session_handle);

// Keys

int session_add_user_key(
    intptr_t session_handle,
    ByteArray userKey
);

int session_add_armored_locked_user_key(
    intptr_t session_handle,
    ByteArray armoredUserKey
);

// Nodes

int node_decrypt_armored_name(
    intptr_t client_handle,
    ByteArray pointer,
    AsyncCallback callback
);

// Drive client 

int drive_client_create(
    intptr_t client_handle,
    intptr_t* out_client_handle
);

int drive_client_free(intptr_t client_handle);

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

int downloader_free(intptr_t downloader_handle);

// Uploads

// Response: IntResponse
int uploader_create(
    intptr_t client_handle,
    ByteArray pointer, // FileUploaderCreationRequest
    AsyncCallback callback
);

// Response: FileNode
int uploader_upload_file(
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

int uploader_free(intptr_t uploader_handle);

// Logger

int logger_provider_create(
    /*Log*/Callback log_callback,
    intptr_t* logger_provider_handle
);

#endif PROTON_SDK_H
