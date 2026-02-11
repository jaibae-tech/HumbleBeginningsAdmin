using System;

namespace MapMaker.Shared.Utils
{
    public static class StatHelpers
    {
        public static float[] ComputeQuantiles(float[] values, float[] targetPercents)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty");
            
            if (targetPercents == null || targetPercents.Length == 0)
                throw new ArgumentException("Target percents array cannot be null or empty");

            int n = values.Length;
            float[] sorted = new float[n];
            Array.Copy(values, sorted, n);
            Array.Sort(sorted);

            float[] thresholds = new float[targetPercents.Length];
            
            for (int i = 0; i < targetPercents.Length; i++)
            {
                float percent = targetPercents[i];
                if (percent <= 0f)
                {
                    thresholds[i] = sorted[0];
                }
                else if (percent >= 1f)
                {
                    thresholds[i] = sorted[n - 1];
                }
                else
                {
                    float exactIndex = percent * (n - 1);
                    int lower = (int)Math.Floor(exactIndex);
                    int upper = (int)Math.Ceiling(exactIndex);
                    float fraction = exactIndex - lower;
                    
                    if (lower == upper)
                    {
                        thresholds[i] = sorted[lower];
                    }
                    else
                    {
                        thresholds[i] = sorted[lower] * (1f - fraction) + sorted[upper] * fraction;
                    }
                }
            }
            
            return thresholds;
        }

        public static (float min, float max, float mean, float stdDev) ComputeStats(float[] values)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty");

            float min = float.MaxValue;
            float max = float.MinValue;
            double sum = 0;

            for (int i = 0; i < values.Length; i++)
            {
                float v = values[i];
                if (v < min) min = v;
                if (v > max) max = v;
                sum += v;
            }

            float mean = (float)(sum / values.Length);

            double varianceSum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double diff = values[i] - mean;
                varianceSum += diff * diff;
            }

            float stdDev = (float)Math.Sqrt(varianceSum / values.Length);

            return (min, max, mean, stdDev);
        }

        public static int[] ComputeHistogram(float[] values, int binCount, float minValue, float maxValue)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty");
            
            if (binCount <= 0)
                throw new ArgumentException("Bin count must be positive");

            int[] histogram = new int[binCount];
            float range = maxValue - minValue;
            
            if (range <= 0f)
            {
                histogram[0] = values.Length;
                return histogram;
            }

            for (int i = 0; i < values.Length; i++)
            {
                float normalized = (values[i] - minValue) / range;
                int bin = (int)(normalized * binCount);
                
                if (bin < 0) bin = 0;
                if (bin >= binCount) bin = binCount - 1;
                
                histogram[bin]++;
            }

            return histogram;
        }

        public static float NormalizeSum(float[] values)
        {
            double sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];

            float sumFloat = (float)sum;
            
            if (sumFloat > 0f)
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] /= sumFloat;
            }

            return sumFloat;
        }

        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
