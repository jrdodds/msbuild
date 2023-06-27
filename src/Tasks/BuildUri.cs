﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Tasks
{
    public sealed class BuildUri : TaskExtension
    {
        public ITaskItem[] InputUri { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Gets or sets the scheme name of the URI.
        /// </summary>
        public string UriScheme { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user name associated with the user that accesses the URI.
        /// </summary>
        public string UriUserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password associated with the user that accesses the URI.
        /// </summary>
        public string UriPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Domain Name System (DNS) host name or IP address of a server.
        /// </summary>
        public string UriHost { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the port number of the URI.
        /// </summary>
        public int UriPort { get; set; } = UseDefaultPortForScheme;

        /// <summary>
        /// Gets or sets the path to the resource referenced by the URI.
        /// </summary>
        public string UriPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any query information included in the URI.
        /// </summary>
        public string UriQuery { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fragment portion of the URI.
        /// </summary>
        public string UriFragment { get; set; } = string.Empty;

        [Output]
        public ITaskItem[] OutputUri { get; private set; } = Array.Empty<ITaskItem>();

        public override bool Execute()
        {
            if (InputUri.Length == 0)
            {
                if (HasUriParameter())
                {
                    // Special case for the InputUri empty set when there are uri parameters.
                    // Create a single item from the provided parameters.
                    OutputUri = new ITaskItem[] { CreateUriTaskItem(new UriBuilder()) };
                }
            }
            else
            {
                List<ITaskItem> uris = new();
                foreach (var item in InputUri)
                {
                    if (!string.IsNullOrWhiteSpace(item.ItemSpec))
                    {
                        UriBuilder? builder = CreateUriBuilder(item.ItemSpec);
                        if (builder != null)
                        {
                            uris.Add(CreateUriTaskItem(builder, item));
                        }
                    }
                }

                OutputUri = uris.ToArray();
            }

            return !Log.HasLoggedErrors;
        }

        private bool HasUriParameter()
        {
            return
                !string.IsNullOrEmpty(UriScheme) ||
                !string.IsNullOrEmpty(UriUserName) ||
                !string.IsNullOrEmpty(UriPassword) ||
                !string.IsNullOrEmpty(UriHost) ||
                UriPort != UseDefaultPortForScheme ||
                !string.IsNullOrEmpty(UriPath) ||
                !string.IsNullOrEmpty(UriQuery) ||
                !string.IsNullOrEmpty(UriFragment);
        }

        private UriBuilder? CreateUriBuilder(string uri)
        {
            try
            {
                return new UriBuilder(uri);
            }
            catch (UriFormatException)
            {
                return null;
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }

        private ITaskItem CreateUriTaskItem(UriBuilder builder, ITaskItem? item = null)
        {
            // Scheme
            if (!string.IsNullOrEmpty(UriScheme))
            {
                // The Scheme property setter throws an ArgumentException for an invalid scheme.
                builder.Scheme = UriScheme;
                // If a scheme has been provided and a port has not, use the default port for the scheme.
                // (This is for the case where the UriBuilder was constructed with an ItemSpec. The port will have been set for the scheme used in the ItemSpec.)
                if (UriPort == UseDefaultPortForScheme && builder.Port != UseDefaultPortForScheme)
                {
                    builder.Port = UseDefaultPortForScheme;
                }
            }

            // UserName
            if (!string.IsNullOrEmpty(UriUserName))
            {
                builder.UserName = UriUserName;
            }

            // Password
            if (!string.IsNullOrEmpty(UriPassword))
            {
                builder.Password = UriPassword;
            }

            // Host
            if (!string.IsNullOrEmpty(UriHost))
            {
                builder.Host = UriHost;
            }

            // Port
            // If a scheme was provided and a port was not, then UriPort and builder.Port will both be -1.
            if (UriPort != builder.Port)
            {
                // The Port property setter throws an ArgumentOutOfRangeException for a port number less than -1 or greater than 65,535.
                builder.Port = UriPort;
            }

            // Path
            if (!string.IsNullOrEmpty(UriPath))
            {
                builder.Path = UriPath;
            }

            // Query
            if (!string.IsNullOrEmpty(UriQuery))
            {
                builder.Query = UriQuery;
            }

            // Fragment
            if (!string.IsNullOrEmpty(UriFragment))
            {
                builder.Fragment = UriFragment;
            }

            // Create a TaskItem from the UriBuilder and set custom metadata.
            TaskItem uri = (item != null) ?
                new TaskItem(item) { ItemSpec = builder.Uri.AbsoluteUri } :
                new TaskItem(builder.Uri.AbsoluteUri);
            uri.SetMetadata("UriScheme", builder.Scheme);
            uri.SetMetadata("UriUserName", builder.UserName);
            uri.SetMetadata("UriPassword", builder.Password);
            uri.SetMetadata("UriHost", builder.Host);
            uri.SetMetadata("UriHostNameType", Uri.CheckHostName(builder.Host).ToString());
            uri.SetMetadata("UriPort", builder.Port.ToString());
            uri.SetMetadata("UriPath", builder.Path);
            uri.SetMetadata("UriQuery", builder.Query);
            uri.SetMetadata("UriFragment", builder.Fragment);

            return uri;
        }

        private const int UseDefaultPortForScheme = -1;
    }
}