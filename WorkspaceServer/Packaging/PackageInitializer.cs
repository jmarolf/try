// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using MLS.Agent.Tools;

namespace WorkspaceServer.Packaging
{
    public class PackageInitializer : IPackageInitializer
    {
        private readonly Func<DirectoryInfo, Budget, Task> afterCreate;

        public string Template { get; }

        public string ProjectName { get; }

        public PackageInitializer(
            string template, 
            string projectName,
            Func<DirectoryInfo, Budget, Task> afterCreate = null)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(template));
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(projectName));
            }

            this.afterCreate = afterCreate;

            Template = template;

            ProjectName = projectName;
        }

        public virtual async Task Initialize(
            DirectoryInfo directory,
            Budget budget = null)
        {
            budget = budget ?? new Budget();

            var dotnet = new Dotnet(directory);

            var result = await dotnet
                             .New(Template,
                                  args: $"--name \"{ProjectName}\" --output \"{directory.FullName}\"",
                                  budget: budget);
            result.ThrowOnFailure($"Error initializing in {directory.FullName}");

            if (afterCreate != null)
            {
                await afterCreate(directory, budget);
            }
        }
    }
}

