﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.WpfExtensions;
using Image = System.Windows.Controls.Image;
using Window = System.Windows.Window;

namespace SIFT_App;

public partial class MainWindow : Window
{
    private Mat _image1;
    private Mat _image2;
    private List<Mat> _loadedImages = new();

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
                ImgImage1.Source = _image1.ToBitmapSource();
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
                ImgImage2.Source = _image2.ToBitmapSource();
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
            var sift = SIFT.Create(nFeatures: 100, nOctaveLayers: 5, contrastThreshold: 0.04, edgeThreshold: 10, sigma: 1.56);
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

            // Draw a red highlight around the first object in image1
            var rect1 = Cv2.BoundingRect(points1.Take(4).ToArray());
            Cv2.Rectangle(_image1, rect1, new Scalar(0, 0, 255), thickness: 2);

            // Draw a green rectangle around the object in image2
            var rect2 = Cv2.BoundingRect(points2.Take(4).ToArray());
            Cv2.Rectangle(_image2, rect2, new Scalar(0, 255, 0), thickness: 2);

            // Display the angle in the text box and the updated images in image1 and image2
            TxtAngle.Text = angle.ToString();
            ImgImage1.Source = _image1.ToBitmapSource();
            ImgImage2.Source = _image2.ToBitmapSource();
        }
    }

    private void BtnLoadImageSeries_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
        };

        while (openFileDialog.ShowDialog() == true)
        {
            try
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    var image = new Mat(fileName);
                    _loadedImages.Add(image);
                }

                DisplayImages();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while uploading the images: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DisplayImages()
    {
        ImageWrapPanel.Children.Clear();
        foreach (var imageControl in _loadedImages.Select(image => new Image { Source = image.ToBitmapSource(), Width = 250, Height = 500, }))
        {
            ImageWrapPanel.Children.Add(imageControl);
        }
    }

    private void BtnCalculateAngel_Click(object sender, RoutedEventArgs e)
    {
        ImageWrapPanel.Children.Clear();
        
        foreach (var image in _loadedImages)
        {
            var gray = image.CvtColor(ColorConversionCodes.BGR2GRAY);
            var edges = gray.Canny(100, 200);
            var contourImage = image.Clone();

            var contours = Cv2.FindContoursAsArray(edges, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            Cv2.DrawContours(contourImage, contours, -1, Scalar.Red, 2);

            ImageWrapPanel.Children.Add(new Image { Source = contourImage.ToBitmapSource(), Width = 250, Height = 500, Margin = new Thickness(5) });
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

    private void MenuItemSift_Click(object sender, RoutedEventArgs e)
    {
        MenuItemSift.IsChecked = true;
        MenuItemNew.IsChecked = false;

        // Show SIFT window
        GridSift.Visibility = Visibility.Visible;
        GridCDE.Visibility = Visibility.Collapsed;
    }

    private void MenuItemNew_Click(object sender, RoutedEventArgs e)
    {
        MenuItemSift.IsChecked = false;
        MenuItemNew.IsChecked = true;

        // Show New window
        GridSift.Visibility = Visibility.Collapsed;
        GridCDE.Visibility = Visibility.Visible;
    }
}