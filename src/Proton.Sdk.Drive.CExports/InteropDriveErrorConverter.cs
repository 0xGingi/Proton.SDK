﻿using Proton.Sdk.CExports;
using Proton.Sdk.Drive.Verification;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropDriveErrorConverter
{
    private const int UnknownDecryptionErrorPrimaryCode = 0;
    private const int ShareMetadataDecryptionErrorPrimaryCode = 1;
    private const int NodeMetadataDecryptionErrorPrimaryCode = 2;
    private const int FileContentsDecryptionErrorPrimaryCode = 3;
    private const int UploadKeyMismatchErrorPrimaryCode = 4;

    public static void SetDomainAndCodes(Error error, Exception exception)
    {
        switch (exception)
        {
            case NodeMetadataDecryptionException e:
                error.Domain = ErrorDomain.DataIntegrity;
                error.PrimaryCode = NodeMetadataDecryptionErrorPrimaryCode;
                error.SecondaryCode = (long)e.Part;
                break;

            case FileContentsDecryptionException:
                error.Domain = ErrorDomain.DataIntegrity;
                error.PrimaryCode = FileContentsDecryptionErrorPrimaryCode;
                break;

            case NodeKeyAndSessionKeyMismatchException:
                error.Domain = ErrorDomain.DataIntegrity;
                error.PrimaryCode = UploadKeyMismatchErrorPrimaryCode;
                break;

            case SessionKeyAndDataPacketMismatchException:
                error.Domain = ErrorDomain.DataIntegrity;
                error.PrimaryCode = UploadKeyMismatchErrorPrimaryCode;
                break;

            default:
                InteropErrorConverter.SetDomainAndCodes(error, exception);
                break;
        }
    }
}
