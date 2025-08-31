using UnityEngine;

namespace WaveParticleSim
{
    public static class QM
    {
        // Angstroms to Momentum
        // AngstromP is h/d in angstroms
        public static double planckConst = 6.62607015e-34;
        public static double planckConsteV = 4.135667696e-15;
        public static double planckxC = 1.98644586e-25;

        public static float AngstromToP = 6.62607004e-24f;
        public static float eVoltsToP = 5.34429723e-28f;
        public static float keVToP = 5.34429723e-25f;
        // Scaled-up versions by 10^24
        public static float AngstromToPe24 = 6.62607004f; // Converts 1/d (Angstroms) to units of momentum e-24.
        public static float keVToPe24 = 0.534429723f;     // Converts KeV to units of momentum e-24.
        public static float Pe24toKeV = 1.871153413f;     // Converts KeV to units of momentum e-24.
        public static float nmRatioToEv = 1239.8394f;

        public static float AngstromToKev = 12.3983936f;  // 1/d Angstrom as KeV
        public static float nmToKeV = 1.2398394f;         // nanometres wavelength to Kev
        public static float nmFarRed = 750;
        public static float nmFarViolet = 380;
        public static float tHz750nm = 400f;
        public static float tHz725nm = 413.5f;
        public static float tHz700nm = 430f;
        public static float tHz400nm = 750f;
        public static float tHz380nm = 790f;
        public static float evFarRed = nmRatioToEv / nmFarRed;
        public static float evFarViolet = nmRatioToEv / nmFarViolet;
        //public static float evSpectrum = evFarViolet-evFarRed;

        public static string ToDegMinSec(float angRadians)
        {
            float angDeg = Mathf.Abs(angRadians * Mathf.Rad2Deg);
            int deg = (int)angDeg;
            deg %= 360;
            if (deg > 0)
            {
                angDeg -= deg;
                deg *= (int)Mathf.Sign(angRadians);
            }

            angDeg *= 60;
            int min = (int)angDeg;
            if (min > 0)
                angDeg -= min;
            angDeg *= 60;
            int sec = (int)angDeg;
            string s = angRadians < 0 ? "-" : "";
            return string.Format("{0}{1}°{2}ʹ{3}ʺ", s, deg, min, sec);
        }
        public static string ToEngineeringNotation(float d)
        {
            string sRet = "0";
            float exponent = Mathf.Log10(Mathf.Abs(d));
            if (Mathf.Abs(d) >= 1)
            {
                switch ((int)Mathf.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                        sRet = string.Format("{0:.##}", d);
                        break;
                    case 3:
                    case 4:
                    case 5:
                        sRet = string.Format("{0:.##}K", d / 1e3);
                        break;
                    case 6:
                    case 7:
                    case 8:
                        sRet = string.Format("{0:.##}M", d / 1e6);
                        break;
                    case 9:
                    case 10:
                    case 11:
                        sRet = string.Format("{0:.##}G", d / 1e9);
                        break;
                    case 12:
                    case 13:
                    case 14:
                        sRet = string.Format("{0:.##}T", d / 1e12);
                        break;
                    case 15:
                    case 16:
                    case 17:
                        sRet = string.Format("{0:.##}P", d / 1e15);
                        break;
                    case 18:
                    case 19:
                    case 20:
                        sRet = string.Format("{0:.##}E", d / 1e18);
                        break;
                    case 21:
                    case 22:
                    case 23:
                        sRet = string.Format("{0:.##}Z", d / 1e21);
                        break;
                    default:
                        sRet = string.Format("{0:.##}Y", d / 1e24);
                        break;
                }
            }
            else if (Mathf.Abs(d) > 0)
            {
                switch ((int)Mathf.Floor(exponent))
                {
                    case -1:
                    case -2:
                    case -3:
                        sRet = string.Format("{0:.##}m", d * 1e3);
                        break;
                    case -4:
                    case -5:
                    case -6:
                        sRet = string.Format("{0:.##}μ", d * 1e6);
                        break;
                    case -7:
                    case -8:
                    case -9:
                        sRet = string.Format("{0breakn", d * 1e9);
                        break;
                    case -10:
                    case -11:
                    case -12:
                        sRet = string.Format("{0:.##}p", d * 1e12);
                        break;
                    case -13:
                    case -14:
                    case -15:
                        sRet = string.Format("{0:.##}f", d * 1e15);
                        break;
                    case -16:
                    case -17:
                    case -18:
                        sRet = string.Format("{0:.##}a", d * 1e15);
                        break;
                    case -19:
                    case -20:
                    case -21:
                        sRet = string.Format("{0:.##}z", d * 1e15);
                        break;
                    default:
                        sRet = string.Format("{0:.##}y", d * 1e15);
                        break;
                }
            }
            return sRet;
        }

        public static bool loadColourMap(int nSamples, string texName, Material mat)
        {
            if (mat == null)
                return false;
            Color[] texData = new Color[nSamples];
            for (int i = 0; i < nSamples; i++)
            {
                float frac = Mathf.InverseLerp(0f, nSamples, i);
                Color dColour = QM.momentumColour(frac);
                texData[i] = dColour;
            }
            var tex = new Texture2D(nSamples, 1, TextureFormat.RGBAFloat, 0, true);
            tex.SetPixels(0, 0, nSamples, 1, texData, 0);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            mat.SetTexture(texName, tex);
            return true;
        }


        public static Color lerpColour(float frac)
        {
            return spectrumColour(Mathf.Lerp(725, 380, frac));
        }

        public static Color speedColour(float s)
        {
            float frac = (s / 2) + 0.5f;
            float tHz = Mathf.Lerp(tHz725nm, tHz380nm, frac);
            float nm = 299792f / tHz;
            return spectrumColour(nm);
        }

        public static Color momentumColour(float e)
        {
            float tHz = Mathf.Lerp(tHz725nm, tHz380nm, e);
            float nm = 299792f / tHz;
            return spectrumColour(nm);
        }

        public static Color evColour(float eV)
        {
            return spectrumColour(nmRatioToEv / eV);
        }

        public static Color nmColour(float nm)
        {
            return spectrumColour(nm);
        }

        /*
        #' @param wavelength A wavelength value, in nanometers, in the human visual range from 380 nm through 750 nm.
        */

        public static Color spectrumColour(float wavelength, float gamma = 0.8f)
        {
            Color result = Color.white;
            if (wavelength >= 380 & wavelength <= 440)
            {
                float attenuation = 0.3f + 0.7f * (wavelength - 380.0f) / (440.0f - 380.0f);
                result.r = Mathf.Pow(((-(wavelength - 440) / (440 - 380)) * attenuation), gamma);
                result.g = 0.0f;
                result.b = Mathf.Pow((1.0f * attenuation), gamma);
            }

            else if (wavelength >= 440 & wavelength <= 490)
            {
                result.r = 0.0f;
                result.g = Mathf.Pow((wavelength - 440f) / (490f - 440f), gamma);
                result.b = 1.0f;
            }
            else if (wavelength >= 490 & wavelength <= 510)
            {
                result.r = 0.0f;
                result.g = 1.0f;
                result.b = Mathf.Pow(-(wavelength - 510f) / (510f - 490f), gamma);
            }
            else if (wavelength >= 510 & wavelength <= 580)
            {
                result.r = Mathf.Pow((wavelength - 510f) / (580f - 510f), gamma);
                result.g = 1.0f;
                result.b = 0.0f;
            }
            else if (wavelength >= 580f & wavelength <= 645f)
            {
                result.r = 1.0f;
                result.g = Mathf.Pow(-(wavelength - 645f) / (645f - 580f), gamma);
                result.b = 0.0f;
            }
            else if (wavelength >= 645 & wavelength <= 750)
            {
                float attenuation = 0.3f + 0.7f * (750 - wavelength) / (750 - 645);
                result.r = Mathf.Pow(1.0f * attenuation, gamma);
                result.g = 0.0f;
                result.b = 0.0f;
            }
            else
            {
                result.r = 0.0f;
                result.g = 0.0f;
                result.b = 0.0f;
                result.a = 0.1f;
            }
            return result;
        }
    }
}
