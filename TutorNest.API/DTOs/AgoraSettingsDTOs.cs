using System;

namespace TutorNest.API.DTOs
{
    public record AgoraSettingsResponse(
        string AppId,
        string AppCertificate
    );

    public record UpdateAgoraSettingsRequest(
        string AppId,
        string AppCertificate
    );
}
