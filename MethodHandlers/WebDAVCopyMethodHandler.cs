using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    /// This class implements the <c>COPY</c> HTTP method for WebDAV#.
    /// </summary>
    public class WebDavCopyMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {
        /// <summary>
        /// Gets the collection of the names of the HTTP methods handled by this instance.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        public IEnumerable<string> Names
        {
            get
            {
                return new[]
                {
                    "COPY"
                };
            }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">The 
        /// <see cref="IHttpListenerContext" /> object containing both the request and response
        /// objects to use.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavMethodNotAllowedException"></exception>
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {            
            IWebDavStoreItem source = context.Request.Url.GetItem(server, store);
            if (source is IWebDavStoreDocument || source is IWebDavStoreCollection)
                CopyItem(server, context, store, source);
            else
                throw new WebDavMethodNotAllowedException();
        }

        /// <summary>
        /// Copies the item.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <param name="store">The store.</param>
        /// <param name="source">The source.</param>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavForbiddenException"></exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavPreconditionFailedException"></exception>
        private void CopyItem(WebDavServer server, IHttpListenerContext context, IWebDavStore store, IWebDavStoreItem source)
        {
            var destinationUri = GetDestinationHeader(context.Request);
            var destinationParentCollection = GetParentCollection(server, store, destinationUri);

            bool copyContent = (GetDepthHeader(context.Request) != 0);
            bool isNew = true;

            string destinationName = Uri.UnescapeDataString(destinationUri.Segments.Last().TrimEnd('/', '\\'));
            IWebDavStoreItem destination = destinationParentCollection.GetItemByName(destinationName);
            
            if (destination != null)
            {
                if (source.ItemPath == destination.ItemPath)
                    throw new WebDavForbiddenException();
                if (!GetOverwriteHeader(context.Request))
                    throw new WebDavPreconditionFailedException();
                if (destination is IWebDavStoreCollection)
                    destinationParentCollection.Delete(destination);
                isNew = false;
            }

            destinationParentCollection.CopyItemHere(source, destinationName, copyContent);

            if (isNew)
                context.SendSimpleResponse(HttpStatusCode.Created);
            else
                context.SendSimpleResponse(HttpStatusCode.NoContent);
        }
    }
}