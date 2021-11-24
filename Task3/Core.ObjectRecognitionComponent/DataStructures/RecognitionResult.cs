using System;
using System.Collections.Generic;
using System.Text;

namespace Core.ObjectRecognitionComponent.DataStructures {
    public class RecognitionResult {
        public string FilePath { get; }
        public YoloV4Result Prediction { get; }

        public RecognitionResult(string filePath, YoloV4Result prediction) {
            FilePath = filePath;
            Prediction = prediction;
        }
    }
}
