@CD /D "%~dp0"
@title ZipDeploy Command Prompt
@SET PATH=C:\Program Files\dotnet\;%PATH%
type readme.txt
@doskey bc=dotnet clean
@doskey btw=dotnet watch msbuild Build.csproj /p:FilterTestFqn=$1 $2 $3 $4 $5 $6 $7 $8 $9
@doskey bt=dotnet msbuild Build.csproj /p:FilterTestFqn=$1 $2 $3 $4 $5 $6 $7 $8 $9
@doskey bw=dotnet watch msbuild Build.csproj $*
@doskey b=dotnet msbuild Build.csproj $*
@doskey br=dotnet restore Build.csproj $*
@echo.
@echo Aliases:
@echo.
@doskey /MACROS
@CD Build
%comspec%