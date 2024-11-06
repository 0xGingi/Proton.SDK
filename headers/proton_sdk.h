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
    void (*on_progress)(const void*, ByteArray);
    void (*on_success)(const void*, ByteArray);
    void (*on_failure)(const void*, ByteArray); 
    intptr_t cancellation_token_source_handle;
} AsyncCallbackWithProgress;

// Sessions

int session_begin(
    ByteArray pointer, // SessionBeginRequest
    AsyncCallback callback
);

int session_resume(
    ByteArray pointer, // SessionResumeRequest
    intptr_t* session_handle
);

int session_renew(
    ByteArray pointer, // SessionRenewRequest
    intptr_t* new_session_handle
);

int session_end(
    intptr_t session_handle, // SessionEndRequest
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

// Revision reader/writer

int revision_reader_read_to_path(
    intptr_t reader_handle,
    ByteArray pointer, // RevisionReadRequest
    AsyncCallbackWithProgress callback
);

int revision_writer_write_to_path(
    intptr_t writerHandle,
    ByteArray pointer, // RevisionWriteRequest
    AsyncCallbackWithProgress callback
);

int revision_reader_free(intptr_t reader_handle);

// Logger

typedef struct {
    const void* state;
    void (*log_callback)(const void*, ByteArray);
} LogCallback;

int logger_provider_create(
    LogCallback log_callback,
    intptr_t* logger_provider_handle
);

#endif PROTON_SDK_H
