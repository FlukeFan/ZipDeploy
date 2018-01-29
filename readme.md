
[![Build Status](https://ci.appveyor.com/api/projects/status/github/FlukeFan/ZipDeploy?svg=true)](https://ci.appveyor.com/project/FlukeFan/ZipDeploy) <pre>

ZipDeploy
=========

Deploy updates to a running Asp.Net Core IIS application by uploading a zip file.

Building
========

To build, open CommandPrompt.bat, and type 'b'.

Build commands:

br                                      Restore dependencies (execute this first)
b                                       Dev-build
ba                                      Build all (including slow tests)
bw                                      Watch dev-build
bt [test]                               Run tests with filter Name~[test]
btw [test]                              Watch run tests with filter Name~[test]
bc                                      Clean the build outputs
b /t:setApiKey /p:apiKey=[key]          Set the NuGet API key
b /t:push                               Push packages to NuGet and publish them (setApiKey before running this)
