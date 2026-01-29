namespace BionicPRO.Api.Configuration;

public class KeycloakSettings
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = true;

    public string MetadataAddress =>
        $"{Authority}/realms/{Realm}/.well-known/openid-configuration";
}
