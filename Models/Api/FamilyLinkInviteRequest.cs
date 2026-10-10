namespace MedLinkPortal.Models.Api
{
    public class FamilyLinkInviteRequest
    {
        public string? Email { get; set; }
        public string? Relationship { get; set; }
    }

    public class FamilyInviteRespondRequest
    {
        /// <summary>"accept" or "reject"</summary>
        public string? Action { get; set; }
    }
}
