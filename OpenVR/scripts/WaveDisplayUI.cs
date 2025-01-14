using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveDisplayUI : MonoBehaviour
{

    [Tooltip("Wave Display Mesh")] 
    public MeshRenderer thePanel;

    [SerializeField] Toggle togPlay;
    [SerializeField] Toggle togPause;
    [SerializeField] bool playSim;

    [Header("Display Mode")]
    [SerializeField]
    public int displayMode;

    [SerializeField] Toggle togReal;
    bool iHaveTogReal = false;
    [SerializeField] Toggle togImaginary;
    bool iHaveTogIm = false;
    [SerializeField] Toggle togRealPwr;
    bool iHaveTogRealPwr = false;
    [SerializeField] Toggle togImPwr;
    bool iHaveTogImPwr = false;
    [SerializeField] Toggle togAmplitude;
    bool iHaveTogAmp = false;
    [SerializeField] Toggle togProbability;
    bool iHaveTogProb = false;

    [SerializeField] private float frequency;
    [SerializeField] Slider frequencySlider;
    [SerializeField] TextMeshProUGUI frequencyValue;

    [SerializeField] private float contrast = 40f;
    [SerializeField] Slider contrastSlider;
    [SerializeField] TextMeshProUGUI contrastValue;

    [Header("Serialized for monitoring in Editor")]
    [SerializeField]
    private Material matSimDisplay = null;
    [SerializeField, Tooltip("Check to invoke CRT Update")]
    private bool iHaveDisplayMaterial = false;
    //[SerializeField]

    private float prevVisibility = -1;
    private void reviewContrast()
    {
        if (!iHaveDisplayMaterial)
            return;
        float targetViz = (contrast / 50);
        if (targetViz == prevVisibility)
            return;
        prevVisibility = targetViz;
        matSimDisplay.SetFloat("_Brightness", targetViz);
    }

    public float Contrast
    {
        get => contrast;
        set
        {
            contrast = value;
            if (contrastSlider != null && contrastSlider.value != value)
                contrastSlider.SetValueWithoutNotify(contrast);
            if (contrastValue != null)
                contrastValue.text = string.Format("{0}%",Mathf.RoundToInt(contrast));
            reviewContrast();
        }
    }

    private void UpdateFrequency()
    {
        if (iHaveDisplayMaterial)
            matSimDisplay.SetFloat("_Frequency", playSim ? frequency : 0f);
    }

    float Frequency
    {
        get => frequency;
        set
        {
            frequency = value;
            if (frequencySlider != null && frequencySlider.value != frequency)
                frequencySlider.SetValueWithoutNotify(frequency);
            if (frequencyValue != null)
                frequencyValue.text = string.Format("{0:0.0}Hz", frequency);
            UpdateFrequency();
        }
    }

    private bool PlaySim
    {
        get => playSim;
        set
        {
            playSim = value;
            if (togPlay != null && value && !togPlay.isOn)
                togPlay.SetIsOnWithoutNotify(true);
            if (togPause != null && !value && !togPause.isOn)
                togPause.SetIsOnWithoutNotify(true);
            UpdateFrequency();
        }
    }

    private void updateDisplayTxture(int displayMode)
    {
        if (!iHaveDisplayMaterial)
        {
            Debug.LogWarning(gameObject.name + ": no Display material");
            return;
        }
        //Debug.Log(gameObject.name + "updateDisplayTxture(Mode 2#" + displayMode.ToString() + ")");
        matSimDisplay.SetFloat("_ShowCRT", displayMode >= 0 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowReal", displayMode == 0 || displayMode == 1 || displayMode >= 4 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowImaginary", displayMode == 2 || displayMode == 3 || displayMode >= 4 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowSquare", displayMode == 1 || displayMode == 3 || displayMode == 5 ? 1f : 0f);
    }
    private int DisplayMode
    {
        get => displayMode;
        set
        {
            displayMode = value;
            updateDisplayTxture(displayMode);
            switch (displayMode)
            {
                case 0:
                    if (iHaveTogReal && !togReal.isOn)
                        togReal.SetIsOnWithoutNotify(true);
                    break;
                case 1:
                    if (iHaveTogRealPwr&& !togRealPwr.isOn)
                        togRealPwr.SetIsOnWithoutNotify(true);
                    break;
                case 2:
                    if (iHaveTogIm && !togImaginary.isOn)
                        togImaginary.SetIsOnWithoutNotify(true);
                    break;
                case 3:
                    if (iHaveTogImPwr && !togImPwr.isOn)
                        togImPwr.SetIsOnWithoutNotify(true);
                    break;
                case 4:
                    if (iHaveTogAmp && !togAmplitude.isOn)
                        togAmplitude.SetIsOnWithoutNotify(true);
                    break;
                default:
                    if (iHaveTogProb && !togProbability.isOn)
                        togProbability.SetIsOnWithoutNotify(true);
                    break;
            }
        }
    }

    public void onPlaySim()
    {
        if (togPlay == null) 
            return;
        if (togPlay.isOn && !playSim)
        {
            PlaySim = true;
        }
    }

    public void onPauseSim()
    {
        if (togPause == null)
            return;
        if (togPause.isOn && playSim)
        {
            PlaySim = false;
        }
    }

    public void onFrequency()
    {
        if (frequencySlider == null)
            return;
        Frequency = frequencySlider.value;
    }

    public void onContrast()
    {
        if (contrastSlider == null)
            return;
        Contrast = contrastSlider.value;
    }

    public void onMode()
    {
        if (iHaveTogReal && togReal.isOn && displayMode != 0)
        {
            DisplayMode = 0;
            return;
        }
        if (iHaveTogIm && togImaginary.isOn && displayMode != 2)
        {
            DisplayMode = 2;
            return;
        }
        if (iHaveTogRealPwr && togRealPwr.isOn && displayMode != 1)
        {
            DisplayMode = 1;
            return;
        }
        if (iHaveTogImPwr && togImPwr.isOn && displayMode != 3)
        {
            DisplayMode = 3;
            return;
        }
        if (iHaveTogAmp && togAmplitude.isOn && displayMode != 4)
        {
            DisplayMode = 4;
            return;
        }
        if (iHaveTogProb && togProbability.isOn && displayMode != 5)
        {
            DisplayMode = 5;
            return;
        }
    }


    private void Awake()
    {
        iHaveTogReal = togReal != null;
        iHaveTogIm = togImaginary != null;
        iHaveTogRealPwr = togRealPwr != null;
        iHaveTogImPwr = togImPwr != null;
        iHaveTogProb = togProbability != null;
        iHaveTogAmp = togAmplitude != null;

        if (thePanel != null)
            matSimDisplay = thePanel.material;
        iHaveDisplayMaterial = matSimDisplay != null;
        if (iHaveDisplayMaterial)
            frequency = matSimDisplay.GetFloat("_Frequency");
    }
    void Start()
    {
        Contrast = contrast;
        Frequency = frequency;
        PlaySim = playSim;
        if (iHaveDisplayMaterial && displayMode < 0)
        {
            int dMode = Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowReal")) > 0 ? 1 : 0;
            dMode += Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowImaginary")) > 0 ? 2 : 0;

            int nSq = Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowSquare")) > 0 ? 1 : 0;
            switch (dMode)
            {
                case 1:
                    displayMode = nSq;
                    break;
                case 2: 
                    displayMode = 2 + nSq;
                    break;
                case 3:
                    displayMode = 4 + nSq;
                    break;
                default:
                    displayMode = 0;
                    break;
            }
        }
        DisplayMode = displayMode;
    }
}
