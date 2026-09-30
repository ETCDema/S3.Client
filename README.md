# S3.Client [EN]

A tiny library for working with services that support the S3 API. It focuses on minimal size and basic functionality.

It uses HTTP/2 by default, but can also work over HTTP/1.1 with both HTTP and HTTPS.

## Tested against

* MinIO
* VaultS3

## Supported operations

* ListObjectsV2
* ListObjectVersions
* GetObject
* HeadObject
* GetObjectTagging
* PutObject
* PutObject with `x-amz-tagging`
* PutObjectTagging
* MultipartUpload: CreateMultipartUpload, UploadPart, CompleteMultipartUpload, AbortMultipartUpload
* CopyObject
* DeleteObject
* DeleteObjectTagging
* DeleteObjects (batch)
* RestoreObject

## Usage

1. Install the package using [NuGet](http://www.nuget.org/packages/Dm.S3.Client/)
```shell
dotnet add package Dm.S3.Client
```
2. Configure the client
```csharp
var credential  = new S3Credential(AccessKey, SecretKey);
var svc         = new S3Service(Endpoint, credential);
var client      = new S3Client(svc);
```
3. Get a bucket and work with it
```csharp
var bucket       = client.GetBucket("my-bucket");

// Get the object with content
using var result = await bucket.GetObject("my-file.bin");
var content      = await result.ReadAsByteArray();
```

A more complete example of using the client can be found in the [S3.Client.Test](https://github.com/ETCDema/S3.Client/tree/main/S3.Client.Test) project.

# S3.Client [RU]

Маленькая библиотека для работы с сервисами, поддерживающими S3 API. Она ориентирована на минимальный размер и базовую функциональность.

По умолчанию использует HTTP/2, но также может работать по HTTP/1.1 как с HTTP, так и с HTTPS.

## Протестировано с

* MinIO
* VaultS3

## Поддерживаемые операции

* ListObjectsV2
* ListObjectVersions
* GetObject
* HeadObject
* GetObjectTagging
* PutObject
* PutObject с тегами в загловке `x-amz-tagging`
* PutObjectTagging
* MultipartUpload: CreateMultipartUpload, UploadPart, CompleteMultipartUpload, AbortMultipartUpload
* CopyObject
* DeleteObject
* DeleteObjectTagging
* DeleteObjects (пакетное удаление)
* RestoreObject

## Использование

1. Установите пакет [NuGet](http://www.nuget.org/packages/Dm.S3.Client/)
```shell
dotnet add package Dm.S3.Client
```
2. Создайте экземпляр клиента с нужными параметрами
```csharp
var credential  = new S3Credential(AccessKey, SecretKey);
var svc         = new S3Service(Endpoint, credential);
var client      = new S3Client(svc);
```
3. Получите объект для работы с бакетом и используйте его для работы с объектами
```csharp
var bucket       = client.GetBucket("my-bucket");

// Получить объект и его содержимое
using var result = await bucket.GetObject("my-file.bin");
var content      = await result.ReadAsByteArray();
```

Примеры использования можно увидеть в тестовом проекте [S3.Client.Test](https://github.com/ETCDema/S3.Client/tree/main/S3.Client.Test).
