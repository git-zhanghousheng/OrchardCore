using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Liquid;
using OrchardCore.DisplayManagement.Liquid.Tags;
using OrchardCore.Liquid;

namespace OrchardCore.Media.Liquid
{
    public class MediaAnchorTag : IAnchorTag
    {
        public int Order => -20;

        public bool Match(List<FilterArgument> argumentsList)
        {
            foreach (var argument in argumentsList)
            {
                switch (argument.Name)
                {
                    case "asset-href": return true;
                }
            }

            return false;
        }

        public async ValueTask<Completion> WriteToAsync(List<FilterArgument> argumentsList, IReadOnlyList<Statement> statements, TextWriter writer, TextEncoder encoder, LiquidTemplateContext context)
        {
            var services = context.Services;
            var viewContext = context.ViewContext;
            var mediaFileStore = services.GetRequiredService<IMediaFileStore>();
            var fileVersionProvider = services.GetRequiredService<IFileVersionProvider>();
            var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();

            string assetHref = null;
            string appendVersion = null;

            Dictionary<string, string> routeValues = null;
            Dictionary<string, string> customAttributes = null;

            foreach (var argument in argumentsList)
            {
                switch (argument.Name)
                {
                    case "asset_href": assetHref = (await argument.Expression.EvaluateAsync(context)).ToStringValue(); break;
                    case "append_version": appendVersion = (await argument.Expression.EvaluateAsync(context)).ToStringValue(); break;

                    default:

                        if (argument.Name.StartsWith("route_", StringComparison.OrdinalIgnoreCase))
                        {
                            routeValues ??= new Dictionary<string, string>();
                            routeValues[argument.Name.Substring(6)] = (await argument.Expression.EvaluateAsync(context)).ToStringValue();
                        }
                        else
                        {
                            customAttributes ??= new Dictionary<string, string>();
                            customAttributes[argument.Name] = (await argument.Expression.EvaluateAsync(context)).ToStringValue();
                        }
                        break;
                }
            }
            if (String.IsNullOrEmpty(assetHref))
            {
                return Completion.Normal;
            }

            var resolvedUrl = mediaFileStore != null ? mediaFileStore.MapPathToPublicUrl(assetHref) : assetHref;

            if (appendVersion != null && fileVersionProvider != null)
            {
                customAttributes["href"] = fileVersionProvider.AddFileVersionToPath(httpContextAccessor.HttpContext.Request.PathBase, resolvedUrl);
            }
            else
            {
                customAttributes["href"] = resolvedUrl;
            }

            var tagBuilder = new TagBuilder("a");

            foreach (var attribute in customAttributes)
            {
                tagBuilder.Attributes[attribute.Key] = attribute.Value;
            }

            tagBuilder.RenderStartTag().WriteTo(writer, (HtmlEncoder)encoder);

            ViewBufferTextWriterContent content = null;

            if (statements != null && statements.Count > 0)
            {
                content = new ViewBufferTextWriterContent();

                var completion = await statements.RenderStatementsAsync(content, encoder, context);

                if (completion != Completion.Normal)
                {
                    return completion;
                }
            }

            if (content != null)
            {
                content.WriteTo(writer, (HtmlEncoder)encoder);
            }

            tagBuilder.RenderEndTag().WriteTo(writer, (HtmlEncoder)encoder);

            return Completion.Normal;
        }
    }
}