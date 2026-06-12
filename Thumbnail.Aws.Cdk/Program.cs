using Amazon.CDK;

namespace Thumbnail.Aws.Cdk
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new ThumbnailStack(app, "ThumbnailStack", new StackProps
            {
            });
            app.Synth();
        }
    }
}
