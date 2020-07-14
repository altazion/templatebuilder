FROM mcr.microsoft.com/dotnet/core/aspnet:3.1


COPY program.dll /program.dll
COPY program.runtimeconfig.json /program.runtimeconfig.json
COPY TemplateBuilder.dll /TemplateBuilder.dll
COPY Octokit.dll /Octokit.dll 
COPY template_schema.xsd /template_schema.xsd

RUN chmod +x /program.dll
RUN chmod +x /program.runtimeconfig.json

#ENTRYPOINT [ "dotnet", "/program.dll" ]