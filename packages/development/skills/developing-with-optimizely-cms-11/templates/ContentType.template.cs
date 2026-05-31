// Template: Optimizely/Episerver CMS 11 content types (page + block) — EPiServer.* 11.x on .NET Framework 4.x
// Packages: EPiServer.CMS.Core 11.x (net461). Docs say "Optimizely"; code/namespaces are "EPiServer".
// Placeholders: {{Namespace}} {{Page}} {{Block}}
//   {{Namespace}} -> Acme.Web    {{Page}} -> StandardPage    {{Block}} -> HeroBlock
//
// NOTE: every persisted property MUST be `public virtual` — Optimizely creates a runtime proxy
//       that intercepts get/set. A non-virtual property silently does not persist.
//       Generate a UNIQUE GUID per content type (e.g. via Visual Studio "Create GUID" / uuidgen).
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace {{Namespace}}.Models.Pages
{
    [ContentType(
        DisplayName = "{{Page}}",
        GUID = "00000000-0000-0000-0000-000000000000",   // <-- replace with a unique GUID
        Description = "A general-purpose content page.",
        GroupName = "{{Namespace}}")]
    public class {{Page}} : PageData
    {
        [Display(Name = "Heading", Order = 10)]
        [CultureSpecific]
        [Required]
        public virtual string Heading { get; set; }

        [Display(Name = "Main body", Order = 20)]
        [CultureSpecific]
        public virtual XhtmlString MainBody { get; set; }

        [Display(Name = "Main content area", Order = 30)]
        public virtual ContentArea MainContentArea { get; set; }

        [Display(Name = "Teaser image", Order = 40)]
        [UIHint(UIHint.Image)]
        public virtual ContentReference TeaserImage { get; set; }
    }
}

namespace {{Namespace}}.Models.Blocks
{
    [ContentType(
        DisplayName = "{{Block}}",
        GUID = "00000000-0000-0000-0000-000000000001",   // <-- replace with a unique GUID
        Description = "A reusable hero block.",
        GroupName = "{{Namespace}}")]
    public class {{Block}} : BlockData
    {
        [Display(Name = "Title", Order = 10)]
        [CultureSpecific]
        [Required]
        public virtual string Title { get; set; }

        [Display(Name = "Image", Order = 20)]
        [UIHint(UIHint.Image)]
        public virtual ContentReference Image { get; set; }

        [Display(Name = "Call to action", Order = 30)]
        public virtual Url CtaLink { get; set; }
    }
}
