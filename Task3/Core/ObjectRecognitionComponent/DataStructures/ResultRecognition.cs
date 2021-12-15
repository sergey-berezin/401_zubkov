using System;
using System.Collections.Generic;
using System.Text;


namespace Core.ObjectRecognitionComponent.DataStructures {
    public class ResultRecognition {
        public string ImagePath { get; }
        public float[] BBox { get; }
        public string Label { get; }
        public float Confidence { get; }

        internal ResultRecognition(string image_path, float[] bbox, string label, float confidence) {
            ImagePath = image_path;
            BBox = bbox;
            Label = label;
            Confidence = confidence;
        }

        internal ResultRecognition(string image_path, YoloV4Result yolovr_result) {
            ImagePath = image_path;
            BBox = yolovr_result.BBox;
            Label = yolovr_result.Label;
            Confidence = yolovr_result.Confidence;
        }
    }
}
