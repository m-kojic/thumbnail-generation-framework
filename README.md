# Thumbnail Generation Framework

This repository contains a reusable C# framework designed for processing video thumbnails, featuring a decoupled architecture that supports multiple deployment models, including local console applications and distributed AWS environments, without changing the core business logic.

## Purpose & Philosophy

This solution implements a **thumbnail processing pipeline** based on a "Discover → Schedule → Generate → Store" workflow. 

By applying Clean Architecture and Hexagonal (Ports & Adapters) principles, the framework ensures that business logic remains completely independent from infrastructure (like AWS SDKs, local filesystems, or ffmpeg), making the entire pipeline highly testable and extensible.

## Solution Structure

The project is structured into modular components:

### Core Library (`Thumbnail.Core`)
Contains the foundational business logic, domain models, and interfaces. It has no external dependencies.
- **Responsibilities:** Defining processing pipelines, thumbnail generation policies, business domain objects, and core abstractions (`IFileScanner`, `IThumbnailGenerator`, `IQueuePublisher`, etc.).

### Infrastructure (`Thumbnail.Infrastructure`)
Contains concrete implementations of the abstractions defined in `Thumbnail.Core`.
- **Responsibilities:** Implementing storage providers (Local FS, S3), thumbnail generation (via `ffmpeg`), logging, and messaging services (SQS).

### Application Hosts
These projects host the pipeline, providing different deployment targets:

- **`Thumbnail.Console`**: A local implementation that runs the processing pipeline on the local machine using parallel scanning and a worker pool with bounded channels for backpressure management.
- **`Thumbnail.Aws.Scanner`**: An AWS Lambda function responsible for scanning an S3 bucket, filtering for video files, and publishing thumbnail processing requests to an SQS queue.
- **`Thumbnail.Aws.Worker`**: An AWS Lambda function triggered by SQS messages that downloads the video, generates the thumbnail, and uploads the result back to S3.

### IaC - Infrastructure as Code (`Thumbnail.Aws.Cdk`)
All the infrastructure is defined in the code and can be easily deployed to an AWS account using CloudFormation.

### Testing (`Thumbnail.Tests`)
Contains unit, integration, and end-to-end tests to ensure pipeline correctness, concurrency safety, and infrastructure reliability.

## Architecture & Dependency Rule

The design enforces strict dependency flow, ensuring that higher-level business logic is never dependent on low-level infrastructure details:

`Console/Lambda Hosts → Infrastructure → Core → Domain`

## Key Features

- **Decoupled Architecture**: Easily switch or add infrastructure implementations (e.g., Azure Blob Storage, different video processors) without modifying business logic.
- **Scalable Design**: 
    - **Local**: Uses bounded channels and worker pools to manage concurrency and memory usage.
    - **Distributed**: Leverages AWS Lambda and SQS for horizontal scalability in the cloud.
- **Backpressure**: Prevents system overload by using bounded queues between scanning and generation.
- **Testability**: Interfaces are designed to be easily mocked, allowing for robust unit and integration testing.
