using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Content.Version1;

namespace Telligent.Evolution.Extensions.SharePoint.Client
{
    internal class RenderedSearchResultOptions
    {
        public RenderedSearchResultOptions()
        {
            Target = Target.Web;
        }

        public Target Target { get; set; }
        public string AvatarUrl { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime? Date { get; set; }
        public Telligent.Evolution.Extensibility.Api.Entities.Version1.User User { get; set; }
        public string TypeCssClass { get; set; }
        public string ContainerName { get; set; }
        public string ContainerUrl { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationUrl { get; set; }
        public List<RenderedSearchResultAttachment> RemoteAttachments { get; set; }
        public List<RenderedSearchResultAttachment> LocalAttachments { get; set; }
    }

    internal class RenderedSearchResultAttachment
    {
        public string FileName { get; set; }
        public string Url { get; set; }
    }

    internal static class HtmlExtensions
    {
        private const string Ellipsis = "...";
        private const int TruncationLength = 250;

        public static string ToRenderedSearchResult(this IContent content, RenderedSearchResultOptions options = null)
        {
            options = options ?? new RenderedSearchResultOptions();

            var avatarSize = options.User == null ? 88 : 44;
            var id = string.Format("searchresult_{0}", Guid.NewGuid().ToString("N"));

            var result = HtmlBuilder.Construct(html =>
            {
                html.Node("div", new { Class = String.Format("content abbreviated rendered {0}", (options.TypeCssClass ?? "")), id }, () =>
                {
                    if (options.User != null)
                    {
                        html.Node("div", new { Class = "author" }, () =>
                        {
                            html.Text(options.User.ToAvatarProfileLinkHtml(size: avatarSize, resizeMethod: "ZoomAndCrop"));
                            html.Text(options.User.ToProfileLinkHtml());
                        });
                    }
                    else if (!string.IsNullOrEmpty(options.AvatarUrl))
                    {
                        html.Node("div", new { Class = "avatar" }, () =>
                        {
                            html.Node("a", new { href = PublicApi.Html.EncodeAttribute(options.Url) }, () =>
                            {
                                html.Text(PublicApi.UI.GetResizedImageHtml(options.AvatarUrl, avatarSize, avatarSize, new UiGetResizedImageHtmlOptions { OutputIsPersisted = false, ResizeMethod = "ZoomAndCrop" }));
                            });
                        });
                    }

                    if (options.Date.HasValue
                        || (!string.IsNullOrEmpty(options.ApplicationName) && !string.IsNullOrEmpty(options.ApplicationUrl))
                        || (!string.IsNullOrEmpty(options.ContainerName) && !string.IsNullOrEmpty(options.ContainerUrl)))
                    {
                        html.Node("div", new { Class = "attributes" }, () =>
                        {
                            html.Node("ul", new { Class = "attribute-list" }, () =>
                            {
                                if (options.Date.HasValue)
                                {
                                    html.Node("li", new { Class = "attribute-item date" }, PublicApi.Language.FormatAgoDate(options.Date.Value));
                                }
                                if (!string.IsNullOrEmpty(options.ContainerName) && !string.IsNullOrEmpty(options.ContainerUrl))
                                {
                                    html.Node("li", new { Class = "attribute-item container" }, () =>
                                    {
                                        html.Text(string.Format("<a href='{0}'>{1}</a>", PublicApi.Html.EncodeAttribute(options.ContainerUrl), options.ContainerName));
                                    });
                                }
                                if (!string.IsNullOrEmpty(options.ApplicationName) && !string.IsNullOrEmpty(options.ApplicationUrl))
                                {
                                    html.Node("li", new { Class = "attribute-item application" }, () =>
                                    {
                                        html.Text(string.Format("<a href='{0}'>{1}</a>", PublicApi.Html.EncodeAttribute(options.ApplicationUrl), options.ApplicationName));
                                    });
                                }
                            });
                        });
                    }

                    html.Node("h3", new { Class = "name" }, () =>
                    {
                        html.Node("a", new
                        {
                            Class = "internal-link",
                            title = PublicApi.Html.EncodeAttribute(PublicApi.Language.Truncate(PublicApi.Html.Decode(options.Title), TruncationLength, Ellipsis)),
                            href = PublicApi.Html.EncodeAttribute(options.Url)
                        }, PublicApi.Language.Truncate(options.Title, TruncationLength, Ellipsis));
                    });

                    html.Node("div", new { Class = "content" }, () =>
                    {
                        html.Text(PublicApi.Language.Truncate(options.Body, TruncationLength, Ellipsis));
                    });

                    if (options.LocalAttachments != null && options.LocalAttachments.Any())
                    {
                        foreach (var attachment in options.LocalAttachments)
                        {
                            var isViewable = PublicApi.UI.GetMediaType(attachment.Url, new UiGetMediaTypeOptions { ViewType = "View", OutputIsPersisted = false }) != "Empty" && options.Target == Target.Web;

                            html.Node("div", new { Class = "attachment" }, () =>
                            {
                                html.Node("ul", new { Class = "navigation-list" }, () =>
                                {
                                    html.Node("li", new { Class = "navigation-list-item" }, (!string.IsNullOrEmpty(attachment.FileName) ? PublicApi.Html.Encode(attachment.FileName) : attachment.Url));
                                    if (isViewable)
                                    {
                                        html.Node("li", new { Class = "navigation-list-item view-attachment", style = "display: list-item" }, () =>
                                        {
                                            html.Node("a", new { href = "#" }, Telligent.Evolution.Components.ResourceManager.GetString("SearchResult_ShowAttachment"));
                                        });
                                        html.Node("li", new { Class = "navigation-list-item hide-attachment hidden", style = "display: none" }, () =>
                                        {
                                            html.Node("a", new { href = "#" }, Telligent.Evolution.Components.ResourceManager.GetString("SearchResult_HideAttachment"));
                                        });
                                    }
                                });

                                if (isViewable)
                                {
                                    html.Node("div", new { Class = "viewer hidden", style = "display: none;" }, PublicApi.UI.GetViewHtml(attachment.Url, new UiGetViewHtmlOptions { AdjustToContainer = true, OutputIsPersisted = false }));
                                    html.Node("script", new { type = "text/javascript" }, string.Format(@"
(function($) {{
	$('#{0} .view-attachment a').click(function() {{ 
		$('#{0} .viewer, #{0} .hide-attachment').removeClass('hidden').show();
		$(this).parent().addClass('hidden').hide();
		return false;
	}});
	$('#{0} .hide-attachment a').click(function() {{
		$('#{0} .viewer').addClass('hidden').hide();
		$('#{0} .view-attachment').removeClass('hidden').show();
		$(this).parent().addClass('hidden').hide();
		return false;
	}});
}})(jQuery);
", id));
                                }
                            });
                        }
                    }

                    if (options.RemoteAttachments != null && options.RemoteAttachments.Any())
                    {
                        html.Node("div", new { Class = "attachment" }, () =>
                        {
                            html.Node("ul", new { Class = "navigation-list" }, () =>
                            {
                                foreach (var attachment in options.RemoteAttachments)
                                {
                                    html.Node("li", new { Class = "navigation-list-item" }, () =>
                                    {
                                        html.Node("a", new { href = attachment.Url, target = "_blank" }, attachment.FileName);
                                    });
                                }
                            });
                        });
                    }
                });
            });

            return result;
        }

        public static string ToProfileLinkHtml(this Telligent.Evolution.Extensibility.Api.Entities.Version1.User user)
        {
            if (user == null) return null;

            return HtmlBuilder.Construct(html =>
            {
                html.Node("span", new { Class = "user-name" }, () =>
                {
                    if (!String.IsNullOrEmpty(user.ProfileUrl))
                    {
                        html.Node("a", new { href = PublicApi.Html.EncodeAttribute(user.ProfileUrl), Class = "internal-link view-user-profile" }, () =>
                        {
                            html.Text(user.DisplayName);
                        });
                    }
                    else
                    {
                        html.Text(user.DisplayName);
                    }
                });
            });
        }

        public static string ToAvatarProfileLinkHtml(this Telligent.Evolution.Extensibility.Api.Entities.Version1.User user, int size = 50, string resizeMethod = "ZoomAndCrop")
        {
            if (user == null)
                return null;

            return HtmlBuilder.Construct(html =>
            {
                html.Node("div", new { Class = "avatar" }, () =>
                {
                    if (!user.IsSystemAccount.GetValueOrDefault(false) && !String.IsNullOrEmpty(user.ProfileUrl))
                    {
                        html.Node("a", new { href = PublicApi.Html.EncodeAttribute(user.ProfileUrl), Class = "internal-link view-user-profile" }, () =>
                        {
                            html.Text(PublicApi.UI.GetResizedImageHtml(user.AvatarUrl, size, size, new Extensibility.Api.Version1.UiGetResizedImageHtmlOptions
                            {
                                ResizeMethod = resizeMethod,
                                HtmlAttributes = new Dictionary<string, string>
                                {
                                    { "border", "0" },
                                    { "alt", user.DisplayName }
                                }
                            }));
                        });
                    }
                    else
                    {
                        html.Text(PublicApi.UI.GetResizedImageHtml(user.AvatarUrl, size, size, new Extensibility.Api.Version1.UiGetResizedImageHtmlOptions
                        {
                            ResizeMethod = "ZoomAndCrop",
                            HtmlAttributes = new Dictionary<string, string>
                                {
                                    { "border", "0" },
                                    { "alt", user.DisplayName }
                                }
                        }));
                    }
                });
            });
        }
    }

    internal class HtmlBuilder
    {
        HtmlElement currentElement;

        private HtmlBuilder(HtmlElement parent)
        {
            currentElement = parent;
        }

        /// <summary>
        /// Constructs a new HTML fragment
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static string Construct(Action<HtmlBuilder> builder)
        {
            var rootElement = new HtmlElement();
            var html = new HtmlBuilder(rootElement);
            builder(html);
            return rootElement.ToString();
        }

        #region Node Construction Overloads

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="selfClosing">Whether the node should self-close with "/&gt;"</param>
        public void Node(string name, bool selfClosing = false)
        {
            AddNode(name, selfClosing: selfClosing);
        }

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="attributes">Object whose properties will be converted to a set of DOM attribute key/value pairs.  Pascal-Cased names will convert to hyphen-cased (DataItem -> data-item)</param>
        /// <param name="selfClosing">Whether the node should self-close with "/&gt;"</param>
        public void Node(string name, object attributes, bool selfClosing = false)
        {
            AddNode(name, attributes: attributes, selfClosing: selfClosing);
        }

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="body">Raw text to set as the inner html of the node</param>
        public void Node(string name, string body)
        {
            AddNode(name, body: body);
        }

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="attributes">Object whose properties will be converted to a set of DOM attribute key/value pairs.  Pascal-Cased names will convert to hyphen-cased (DataItem -> data-item)</param>
        /// <param name="body">Raw text to set as the inner html of the node</param>
        public void Node(string name, object attributes, string body)
        {
            AddNode(name, body: body, attributes: attributes);
        }

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="fragmentBuilder">Calls against Node within this lambda will result in child nodes of the node</param>
        public void Node(string name, Action fragmentBuilder)
        {
            AddNode(name, fragmentBuilder: fragmentBuilder);
        }

        /// <summary>
        /// Adds an HTML node at the current position in the document
        /// </summary>
        /// <param name="name">Element type</param>
        /// <param name="attributes">Object whose properties will be converted to a set of DOM attribute key/value pairs.  Pascal-Cased names will convert to hyphen-cased (DataItem -> data-item)</param>
        /// <param name="fragmentBuilder">Calls against Node within this lambda will result in child nodes of the node</param>
        public void Node(string name, object attributes, Action fragmentBuilder)
        {
            AddNode(name, fragmentBuilder: fragmentBuilder, attributes: attributes);
        }

        /// <summary>
        /// Adds raw text to the current position in the document
        /// </summary>
        /// <param name="value">Raw Text value</param>
        public void Text(string value)
        {
            if (String.IsNullOrEmpty(value))
                return;

            var textElement = new HtmlElement(currentElement);
            textElement.Body = value;
        }

        /// <summary>
        /// Adds an HTML comment to the current position in the document
        /// </summary>
        /// <param name="comment"></param>
        public void Comment(string comment)
        {
            if (String.IsNullOrEmpty(comment))
                return;

            Text(String.Format("<!-- {0} -->", comment));
        }

        #endregion

        #region helpers
        private void AddNode(string name, string body = null, object attributes = null, Action fragmentBuilder = null, bool selfClosing = false)
        {
            var parentNode = currentElement;

            currentElement = new HtmlElement(currentElement)
            {
                Name = name,
                SelfClosing = selfClosing
            };

            if (!String.IsNullOrEmpty(body))
            {
                currentElement.Body = body;
            }
            if (fragmentBuilder != null)
            {
                fragmentBuilder();
            }
            if (attributes != null)
            {
                var properties = attributes.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    currentElement.Attributes[prop.Name] = (prop.GetValue(attributes, null) ?? String.Empty).ToString();
                }
            }

            currentElement = parentNode;
        }

        #endregion

        private class HtmlElement
        {
            public HtmlElement(HtmlElement parentNode = null)
            {
                Children = new List<HtmlElement>();
                Attributes = new Dictionary<string, string>();
                if (parentNode != null)
                    parentNode.Children.Add(this);
            }

            public bool SelfClosing { get; set; }
            public string Name { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
            public List<HtmlElement> Children { get; set; }
            public string Body { get; set; }

            public override string ToString()
            {
                var html = new StringBuilder();
                if (String.IsNullOrEmpty(Name) && Children.Count > 0)
                {
                    Children.ForEach(child => html.Append(child.ToString()));
                }
                else if (String.IsNullOrEmpty(Name) && !String.IsNullOrEmpty(Body))
                {
                    html.Append(Body);
                }
                else if (!String.IsNullOrEmpty(Name))
                {
                    html.AppendFormat("<{0}", Name.ToLowerInvariant());
                    if (Attributes != null && Attributes.Count > 0)
                    {
                        foreach (var key in Attributes.Keys)
                        {
                            html.AppendFormat(" {0}=\"{1}\"", ConvertToDashCase(key), (Attributes[key]));
                        }
                    }
                    if (SelfClosing && String.IsNullOrEmpty(Body) && (Children == null || Children.Count == 0))
                    {
                        html.Append(" />");
                    }
                    else
                    {
                        html.Append(">");
                        if (!String.IsNullOrEmpty(Body))
                        {
                            html.Append(Body);
                        }
                        if (Children != null)
                        {
                            Children.ForEach(child => html.Append(child.ToString()));
                        }
                        html.AppendFormat("</{0}>", Name.ToLowerInvariant());
                    }
                }
                return html.ToString();
            }

            string ConvertToDashCase(string pascalCasedValue)
            {
                if (String.IsNullOrEmpty(pascalCasedValue))
                    return pascalCasedValue;

                var converted = new StringBuilder();
                foreach (char character in pascalCasedValue)
                {
                    if (converted.Length > 0 && character.ToString().ToLowerInvariant() != character.ToString())
                    {
                        converted.AppendFormat("-{0}", character.ToString().ToLowerInvariant());
                    }
                    else
                    {
                        converted.Append(character.ToString().ToLowerInvariant());
                    }
                }
                return converted.ToString();
            }
        }
    }
}
