using System;
using System.Collections.Generic;
using System.Linq;
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
            var sift = SIFT.Create(nFeatures: 0, nOctaveLayers: 3, contrastThreshold: 0.04, edgeThreshold: 10, sigma: 1.6);
            var matcher = new FlannBasedMatcher();

            // Detect keypoints and compute descriptors for both images
            KeyPoint[] keypoints1, keypoints2;
            sift.DetectAndCompute(_image1, null, out keypoints1, descriptors1);
            sift.DetectAndCompute(_image2, null, out keypoints2, descriptors2);

            // Match the descriptors using FLANN-based matching
            var matches = matcher.KnnMatch(descriptors1, descriptors2, k: 2);

            // Filter out matches that are not consistent with a rotation
            var goodMatches = new List<DMatch>();
            foreach (var match in matches)
            {
                if (match[0].Distance < 0.7 * match[1].Distance)
                {
                    var pt1 = keypoints1[match[0].QueryIdx].Pt;
                    var pt2 = keypoints2[match[0].TrainIdx].Pt;

                    // Check if the points are within the boundaries of the objects
                    if (IsPointWithinObject(pt1, _image1) && IsPointWithinObject(pt2, _image2))
                    {
                        goodMatches.Add(match[0]);
                    }
                }
            }

            // Calculate the rotation angle
            var points1 = goodMatches.Select(match => keypoints1[match.QueryIdx].Pt).ToArray();
            var points2 = goodMatches.Select(match => keypoints2[match.TrainIdx].Pt).ToArray();
            var homography = Cv2.FindHomography(InputArray.Create(points1), InputArray.Create(points2),
                HomographyMethods.Ransac);

            var angle = Math.Atan2(homography.At<double>(1, 0), homography.At<double>(0, 0)) * 180 / Math.PI;

            // Draw a green rectangle around the object in image2
            var rect = Cv2.BoundingRect(points2);
            Cv2.Rectangle(_image2, rect, new Scalar(0, 255, 0), thickness: 2);

            // Display the angle in the text box and the updated image in image2
            txtAngle.Text = angle.ToString();
            imgImage2.Source = _image2.ToBitmapSource();
        }
    }

    private bool IsPointWithinObject(Point2f point, Mat image)
    {
        // Define the boundaries of the object as a percentage of the image size
        const double boundaryPercentage = 0.1;

        var minX = (int)(image.Width * boundaryPercentage);
        var maxX = (int)(image.Width * (1 - boundaryPercentage));
        var minY = (int)(image.Height * boundaryPercentage);
        var maxY = (int)(image.Height * (1 - boundaryPercentage));

        return point.X >= minX && point.X <= maxX && point.Y >= minY && point.Y <= maxY;
    }
}