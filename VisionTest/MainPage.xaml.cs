using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Media;
using Xamarin.Forms;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Globalization;
using Acr.UserDialogs;

namespace VisionTest
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(true)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        async void RecognizeFace(object sender, System.EventArgs e)
        {
            try
            {
                await CrossMedia.Current.Initialize();

                if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await DisplayAlert("No Camera", ":( No camera available.", "OK");
                    return;
                }

                var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "FaceDetectionSample.jpg"
                });

                if (file == null)
                    return;
                    
                var image = new Image();

                image.Source = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    return stream;
                });

                var client = new ComputerVisionClient(
                        new ApiKeyServiceClientCredentials(Constants.COMPUTER_VISION_KEY),
                        new System.Net.Http.DelegatingHandler[] { })
                {
                    Endpoint = Constants.COMPUTER_VISION_ROOT
                };

                var description = String.Empty;
                using (UserDialogs.Instance.Loading("Running AI Analysis"))
                {
                    var analysis = await client.AnalyzeImageInStreamAsync(file.GetStream(), getFeatures);

                    foreach (var face in analysis.Faces)
                    {
                        description += face.Gender + " " + face.Age + " ";
                    }
                    foreach (var tag in analysis.Tags)
                    {
                        description += "\n" + tag.Hint + " " + tag.Name + " Confidence: " + tag.Confidence.ToString("P1", CultureInfo.InvariantCulture);
                    }
                }

                await DisplayAlert("Detected Attributes", description, "OK");
            }

            catch (Exception ex) {
                await DisplayAlert("Oh no!!", ex.Message, "OK");
            }
        }

        private static readonly List<VisualFeatureTypes> getFeatures =
            new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags
        };

        // ocr
        async void RecognizeText(object sender, System.EventArgs e)
        {
            try
            {
                await CrossMedia.Current.Initialize();

                if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
                {
                    await DisplayAlert("No Camera", ":( No camera available.", "OK");
                    return;
                }

                var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
                {
                    Directory = "Sample",
                    Name = "FaceDetectionSample.jpg"
                });

                if (file == null)
                    return;
                    
                var image = new Image();

                image.Source = ImageSource.FromStream(() =>
                {
                    var stream = file.GetStream();
                    return stream;
                });

                var client = new ComputerVisionClient(
                        new ApiKeyServiceClientCredentials(Constants.COMPUTER_VISION_KEY),
                        new System.Net.Http.DelegatingHandler[] { })
                {
                    Endpoint = Constants.COMPUTER_VISION_ROOT
                };

                var description = String.Empty;
                BatchReadFileInStreamHeaders textHeaders =
                    await client.BatchReadFileInStreamAsync(
                        file.GetStream());

                using (UserDialogs.Instance.Loading("Recognizing Text"))
                {
                    description = await GetTextAsync(client, textHeaders.OperationLocation);
                }

                await DisplayAlert("Detected Text", description, "OK");
            }

            catch (Exception ex) {
                await DisplayAlert("Oh no!!", ex.Message, "OK");
            }

        }

        // Retrieve the recognized text
        private static async Task<String> GetTextAsync(
            ComputerVisionClient computerVision, string operationLocation)
        {

        int numberOfCharsInOperationId = 36;
        StringBuilder returnString = new StringBuilder();

        // Retrieve the URI where the recognized text will be
        // stored from the Operation-Location header
        string operationId = operationLocation.Substring(
                operationLocation.Length - numberOfCharsInOperationId);

            Console.WriteLine("\nCalling GetHandwritingRecognitionOperationResultAsync()");
            ReadOperationResult result =
                await computerVision.GetReadOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running ||
                    result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                Console.WriteLine(
                    "Server status: {0}, waiting {1} seconds...", result.Status, i);
                await Task.Delay(1000);

                result = await computerVision.GetReadOperationResultAsync(operationId);
            }

            // Display the results
            Console.WriteLine();
            var recResults = result.RecognitionResults;
            foreach (TextRecognitionResult recResult in recResults)
            {
                foreach (Line line in recResult.Lines)
                {
                    returnString.Append(line.Text);
                }
            }
            return returnString.ToString();
        }
    }
}
