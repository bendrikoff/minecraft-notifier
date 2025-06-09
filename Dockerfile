# ���������� .NET SDK 6.0 ��� ������ ����������
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# �������� ����� ������� � ��������� ������
COPY . .
RUN dotnet publish -c Release -o out

# ���������� ASP.NET Core Runtime 6.0 ��� ������� ����������
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# �������� ��������� ����� �� ����������� �����
COPY --from=build /app/out .

# ������ ������� ��� ������� ����������
ENTRYPOINT ["dotnet", "MInecraft Notifier.dll"]
