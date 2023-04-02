using System;
using System.Windows;

using Microsoft.Win32;

using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.WpfExtensions;

using Window = System.Windows.Window;

namespace SIFT_App;

public partial class MainWindow : Window
{
    private Mat _image1;
    private Mat _image2;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void btnUploadImage1_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
            try
            {
                _image1 = new Mat(openFileDialog.FileName);
                imgImage1.Source = _image1.ToBitmapSource();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while uploading the image: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    private void btnUploadImage2_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
            try
            {
                _image2 = new Mat(openFileDialog.FileName);
                imgImage2.Source = _image2.ToBitmapSource();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while uploading the image: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    private void btnCalculateAngle_Click(object sender, RoutedEventArgs e)
    {
        if (_image1 != null && _image2 != null)
        {
            var descriptors1 = new Mat();
            var descriptors2 = new Mat();

            // Initialize SIFT detector and matcher
            var sift = SIFT.Create();
            var matcher = new BFMatcher();

            // Detect keypoints and compute descriptors for both images
            KeyPoint[] keypoints1, keypoints2;
            sift.DetectAndCompute(_image1, null, out keypoints1, descriptors1);
            sift.DetectAndCompute(_image2, null, out keypoints2, descriptors2);

            // Match the descriptors using brute-force matching
            var matches = matcher.Match(descriptors1, descriptors2);

            // Calculate the rotation angle
            var angle = CalculateRotationAngle(keypoints1, keypoints2, matches);

            // Display the angle in the text box
            txtAngle.Text = angle.ToString();
        }
    }

    private double CalculateRotationAngle(KeyPoint[] keypoints1, KeyPoint[] keypoints2, DMatch[] matches)
    {
        // Filter out matches that are not consistent with a rotation
        var numMatches = matches.Length;
        var points1 = new Point2f[numMatches];
        var points2 = new Point2f[numMatches];
        for (var i = 0; i < numMatches; i++)
        {
            points1[i] = keypoints1[matches[i].QueryIdx].Pt;
            points2[i] = keypoints2[matches[i].TrainIdx].Pt;
        }

        var mask = new Mat();

        var homography = Cv2.FindHomography(InputArray.Create(points1), InputArray.Create(points2),
            HomographyMethods.Ransac, 5, mask);

        // Calculate the rotation angle using the homography matrix
        var angle = Math.Atan2(homography.At<double>(1, 0), homography.At<double>(0, 0)) * 180 / Math.PI;
        return angle;
    }
}