using Moq;
using Thumbnail.Core.Interfaces;
using Thumbnail.Core.Services;
using Xunit;

namespace Thumbnail.Tests;

public class ThumbnailServiceTests
{
    [Fact]
    public async Task ProcessDirectoryAsync_CallsGeneratorForSupportedVideos()
    {
        // Arrange
        var mockFs = new Mock<IFileSystem>();
        var mockGenerator = new Mock<IThumbnailGenerator>();
        
        mockFs.Setup(fs => fs.ListFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "video1.mp4", "image1.jpg", "video2.webm" });
            
        var service = new ThumbnailService(mockFs.Object, mockGenerator.Object);

        // Act
        await service.ProcessDirectoryAsync("input", "output");

        // Assert
        mockGenerator.Verify(g => g.GenerateThumbnailAsync("video1.mp4", It.IsAny<string>()), Times.Once);
        mockGenerator.Verify(g => g.GenerateThumbnailAsync("video2.webm", It.IsAny<string>()), Times.Once);
        mockGenerator.Verify(g => g.GenerateThumbnailAsync("image1.jpg", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDirectoryAsync_ContinuesOnError()
    {
        // Arrange
        var mockFs = new Mock<IFileSystem>();
        var mockGenerator = new Mock<IThumbnailGenerator>();
        
        mockFs.Setup(fs => fs.ListFilesAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<string> { "corrupt.mp4", "valid.mp4" });
            
        mockGenerator.Setup(g => g.GenerateThumbnailAsync("corrupt.mp4", It.IsAny<string>()))
            .ThrowsAsync(new Exception("Corrupt file"));
            
        var service = new ThumbnailService(mockFs.Object, mockGenerator.Object);

        // Act
        await service.ProcessDirectoryAsync("input", "output");

        // Assert
        mockGenerator.Verify(g => g.GenerateThumbnailAsync("corrupt.mp4", It.IsAny<string>()), Times.Once);
        mockGenerator.Verify(g => g.GenerateThumbnailAsync("valid.mp4", It.IsAny<string>()), Times.Once);
    }
}
