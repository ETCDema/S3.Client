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
