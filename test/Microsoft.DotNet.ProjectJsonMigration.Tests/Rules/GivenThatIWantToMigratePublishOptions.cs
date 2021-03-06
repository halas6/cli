﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.DotNet.Internal.ProjectModel;
using Microsoft.DotNet.Internal.ProjectModel.Files;
using Microsoft.DotNet.TestFramework;
using Microsoft.DotNet.Tools.Test.Utilities;
using NuGet.Frameworks;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.ProjectJsonMigration.Rules;

namespace Microsoft.DotNet.ProjectJsonMigration.Tests
{
    public class GivenThatIWantToMigratePublishOptions : TestBase
    {
        [Fact]
        private void MigratingPublishOptionsForConsoleAppIncludeExcludePopulatesContentItemWithInclude()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteFilesInProjectDirectory(testDirectory);

            var mockProj = RunPublishOptionsRuleOnPj(@"
                {
                    ""publishOptions"": {
                        ""include"": [""root"", ""src"", ""rootfile.cs""],
                        ""exclude"": [""src"", ""rootfile.cs""],
                        ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                        ""excludeFiles"": [""src/file2.cs""]
                    }
                }",
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(4);

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals("None", StringComparison.Ordinal)))
            {
                item.Metadata.Count(m => m.Name == "CopyToPublishDirectory").Should().Be(1);

                if (item.Update.Contains(@"src\file1.cs"))
                {
                    item.Update.Should().Be(@"src\file1.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file2.cs"))
                {
                    item.Update.Should().Be(@"src\file2.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
                else if (item.Update.Contains(@"root\**\*"))
                {
                    item.Update.Should().Be(@"root\**\*");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else
                {
                    item.Update.Should().Be(@"src\**\*;rootfile.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
            }
        }

        [Fact]
        private void MigratingPublishOptionsForWebAppIncludeExcludePopulatesContentItemWithUpdate()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteFilesInProjectDirectory(testDirectory);

            var mockProj = RunPublishOptionsRuleOnPj(@"
                {
                    ""publishOptions"": {
                        ""include"": [""root"", ""src"", ""rootfile.cs""],
                        ""exclude"": [""src"", ""rootfile.cs""],
                        ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                        ""excludeFiles"": [""src/file2.cs""]
                    },
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }",
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(4);

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals("None", StringComparison.Ordinal)))
            {
                item.Metadata.Count(m => m.Name == "CopyToPublishDirectory").Should().Be(1);

                if (item.Update.Contains(@"src\file1.cs"))
                {
                    item.Update.Should().Be(@"src\file1.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file2.cs"))
                {
                    item.Update.Should().Be(@"src\file2.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
                else if (item.Update.Contains(@"root\**\*"))
                {
                    item.Update.Should().Be(@"root\**\*");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else
                {
                    item.Update.Should().Be(@"src\**\*;rootfile.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
            }
        }

        [Fact]
        private void MigratingConsoleAppWithPublishOptionsAndBuildOptionsCopyToOutputMergesContentItemsWithInclude()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteFilesInProjectDirectory(testDirectory);

            var mockProj = RunPublishAndBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""copyToOutput"": {
                            ""include"": [""src"", ""rootfile.cs""],
                            ""exclude"": [""src"", ""rootfile.cs""],
                            ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                            ""excludeFiles"": [""src/file2.cs""]
                        }
                    },
                    ""publishOptions"": {
                        ""include"": [""root"", ""src"", ""rootfile.cs""],
                        ""exclude"": [""src"", ""rootfile.cs""],
                        ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                        ""excludeFiles"": [""src/file3.cs""]
                    }
                }",
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(5);

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals("None", StringComparison.Ordinal)))
            {
                if (item.Update.Contains(@"root\**\*"))
                {
                    item.Update.Should().Be(@"root\**\*");
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file1.cs"))
                {
                    item.Update.Should().Be(@"src\file1.cs");
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file2.cs"))
                {
                    item.Update.Should().Be(@"src\file2.cs");
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "Never").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file3.cs"))
                {
                    item.Update.Should().Be(@"src\file3.cs");
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
                else
                {
                    item.Update.Should()
                        .Be(@"src\**\*;rootfile.cs");
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "Never").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
            }
        }

        [Fact]
        private void MigratingWebAppWithPublishOptionsAndBuildOptionsCopyToOutputMergesContentItemsWithUpdate()
        {
            var testDirectory = Temp.CreateDirectory().Path;
            WriteFilesInProjectDirectory(testDirectory);

            var mockProj = RunPublishAndBuildOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""copyToOutput"": {
                            ""include"": [""src"", ""rootfile.cs""],
                            ""exclude"": [""src"", ""rootfile.cs""],
                            ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                            ""excludeFiles"": [""src/file2.cs""]
                        },
                        ""emitEntryPoint"": true
                    },
                    ""publishOptions"": {
                        ""include"": [""root"", ""src"", ""rootfile.cs""],
                        ""exclude"": [""src"", ""rootfile.cs""],
                        ""includeFiles"": [""src/file1.cs"", ""src/file2.cs""],
                        ""excludeFiles"": [""src/file3.cs""]
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }",
                testDirectory: testDirectory);

            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(5);

            // From ProjectReader #L725 (Both are empty)
            var defaultIncludePatterns = Enumerable.Empty<string>();
            var defaultExcludePatterns = Enumerable.Empty<string>();

            foreach (var item in mockProj.Items.Where(i => i.ItemType.Equals("None", StringComparison.Ordinal)))
            {
                var metadata = string.Join(",", item.Metadata.Select(m => m.Name));

                if (item.Update.Contains(@"root\**\*"))
                {
                    item.Update.Should().Be(@"root\**\*");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file1.cs"))
                {
                    item.Update.Should().Be(@"src\file1.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file2.cs"))
                {
                    item.Update.Should().Be(@"src\file2.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "Never").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "PreserveNewest").Should().Be(1);
                }
                else if (item.Update.Contains(@"src\file3.cs"))
                {
                    item.Update.Should().Be(@"src\file3.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
                else
                {
                    item.Update.Should().Be(@"src\**\*;rootfile.cs");
                    item.Exclude.Should().BeEmpty();
                    item.Metadata.Count(m =>
                        m.Name == "CopyToOutputDirectory" && m.Value == "Never").Should().Be(1);
                    item.Metadata.Count(m =>
                        m.Name == "CopyToPublishDirectory" && m.Value == "Never").Should().Be(1);
                }
            }
        }

        [Fact]
        public void ExcludedPatternsAreNotEmittedOnNoneWhenBuildingAWebProject()
        {
            var mockProj = RunPublishOptionsRuleOnPj(@"
                {
                    ""buildOptions"": {
                        ""emitEntryPoint"": true
                    },
                    ""publishOptions"": {
                        ""include"": [""wwwroot"", ""**/*.cshtml"", ""appsettings.json"", ""web.config""],
                    },
                    ""dependencies"": {
                        ""Microsoft.AspNetCore.Mvc"" : {
                            ""version"": ""1.0.0""
                        }
                    },
                    ""frameworks"": {
                        ""netcoreapp1.0"": {}
                    }
                }");
 
            mockProj.Items.Count(i => i.ItemType.Equals("None", StringComparison.Ordinal)).Should().Be(0);
        }

        private void WriteFilesInProjectDirectory(string testDirectory)
        {
            Directory.CreateDirectory(Path.Combine(testDirectory, "root"));
            Directory.CreateDirectory(Path.Combine(testDirectory, "src"));
            File.WriteAllText(Path.Combine(testDirectory, "root", "file1.txt"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "root", "file2.txt"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "root", "file3.txt"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "src", "file1.cs"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "src", "file2.cs"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "src", "file3.cs"), "content");
            File.WriteAllText(Path.Combine(testDirectory, "rootfile.cs"), "content");
        }

        private ProjectRootElement RunPublishOptionsRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            return TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigratePublishOptionsRule()
            }, s, testDirectory);
        }

        private ProjectRootElement RunPublishAndBuildOptionsRuleOnPj(string s, string testDirectory = null)
        {
            testDirectory = testDirectory ?? Temp.CreateDirectory().Path;
            return TemporaryProjectFileRuleRunner.RunRules(new IMigrationRule[]
            {
                new MigrateBuildOptionsRule(),
                new MigratePublishOptionsRule()
            }, s, testDirectory);
        }
    }
}
