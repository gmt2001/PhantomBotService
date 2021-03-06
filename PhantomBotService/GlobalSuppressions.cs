﻿
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SG0018:Path traversal", Justification = "Safe usage for log file names", Scope = "member", Target = "~M:PhantomBotService.PhantomBotService.OnStart(System.String[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SG0018:Path traversal", Justification = "Path provided by installer", Scope = "member", Target = "~M:PhantomBotService.PhantomBotServiceInstaller.CreateConfig")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "SG0018:Path traversal", Justification = "Path provided by installer", Scope = "member", Target = "~M:PhantomBotService.PhantomBotServiceInstaller.DeleteConfig")]

