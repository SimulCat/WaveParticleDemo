
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DualParticleWaveUI : MonoBehaviour
{
    [Header("Demo Handlers")]
    [SerializeField]
    QuantumScatter particleSim;
    //[SerializeField]
    //private UdonBehaviour vectorDrawing;

    [SerializeField]
    CustomRenderTexture waveCRT;
    [SerializeField]
    Vector2Int waveCrtSizePx = new Vector2Int(1024, 640);
    [SerializeField]
    Vector2 waveCrtDims = new Vector2(2.56f, 1.6f);
    [SerializeField] private WaveDisplayUI waveDisplayUI;

    [Header("Grating Properties")]
    [SerializeField,Tooltip("Scales control settings in mm to lengths in metres")]
    private float controlScale = 0.001f;
    [SerializeField]
    private int slitCount;
    [SerializeField]
    private float slitWidth;
    [SerializeField]
    private float slitPitch;
    [SerializeField, Range(1, 5)]
    private float simScale;
    [SerializeField]
    private float momentum;
    [SerializeField]
    private float lambda;

    //[SerializeField]
    private bool crtUpdateRequired;

    [Header("UI Elements")]
    // UI
    [SerializeField] private Slider pitchSlider;
    [SerializeField] private TextMeshProUGUI pitchValue;

    [SerializeField] private Slider widthSlider;
    [SerializeField] private TextMeshProUGUI widthValue;
    [SerializeField] private Slider lambdaSlider;
    [SerializeField] private TextMeshProUGUI lblMomentum;
    [SerializeField] private TextMeshProUGUI lblLambda;
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private TextMeshProUGUI scaleValue;
    [SerializeField] private float waveSpeed = 10;
    [SerializeField] private TextMeshProUGUI lblSlitCount;
    [Header("Particle Properties")]

    [SerializeField] private float minLambda = 20;
    [SerializeField] private float maxLambda = 100;
    [Header("Constants"), SerializeField]

    private int MAX_SLITS = 17;

    [Header("Working Value Feedback")]
    //[SerializeField]
    Material matWaveCRT;
    //[SerializeField]
    private float waveMeshScale = 1; // Scales UI control values for wavelength and grating dimensions to CRT gridpoints units

    public Color spectrumColour(float wavelength, float gamma = 0.8f)
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

    [SerializeField]
    Color displayColour;

    public Color DisplayColour
    {
        get => displayColour;
        set
        {
            displayColour = value;
            {
                if (particleSim != null)
                    particleSim.DisplayColor = displayColour;
                if (waveDisplayUI != null)
                    waveDisplayUI.FlowColour = displayColour;
            }
        }
    }
    private void SetColour()
    {
        float frac = Mathf.InverseLerp(minLambda,maxLambda,lambda);
        Color dColour = spectrumColour(Mathf.Lerp(400,700,frac));
        dColour.r = Mathf.Clamp(dColour.r, 0.2f, 2f);
        dColour.g = Mathf.Clamp(dColour.g, 0.2f, 2f);
        dColour.b = Mathf.Clamp(dColour.b, 0.2f, 2f);
        DisplayColour = dColour;
    }

    private void updateLambda()
    {
        if (lblLambda != null)
            lblLambda.text = string.Format("λ={0:0.0}", lambda);
        if (matWaveCRT != null)
        {
            matWaveCRT.SetFloat("_Lambda",lambda * waveMeshScale);
            crtUpdateRequired = true;
        }
        //if (vectorDrawing != null)
        //    vectorDrawing.SetProgramVariable<float>("lambda", lambda);
        if (waveDisplayUI != null)
            waveDisplayUI.Frequency = waveSpeed/lambda;
    }

    public void incSlits()
    {
        SlitCount = slitCount + 1;
    }
    public void decSlits()
    {
        SlitCount = slitCount - 1;
    }

    private void initSimulations()
    {
        if (particleSim != null)
        {
            particleSim.MaxParticleK = 1/(minLambda*controlScale);
            particleSim.SimScale = simScale;
        }
        if (matWaveCRT != null)
        {
            matWaveCRT.SetFloat("_SlitCount", slitCount);
            matWaveCRT.SetFloat("_SlitWidth", slitWidth*waveMeshScale);
            matWaveCRT.SetFloat("_SlitPitch", slitPitch*waveMeshScale);
            matWaveCRT.SetFloat("_Scale", simScale);
            waveCRT.Update(1);
        }
        crtUpdateRequired = false;
    }

    public int SlitCount
    {
        get => slitCount;
        set
        {
            if (value < 1)
                value = 1;
            else if (value > MAX_SLITS)
                value = MAX_SLITS;
            crtUpdateRequired |= value != slitCount;
            slitCount = value;
            if (lblSlitCount != null)
                lblSlitCount.text = value.ToString();
            //if (vectorDrawing != null)
            //    vectorDrawing.SetProgramVariable<int>("slitCount", slitCount);
            if (particleSim != null)
                particleSim.SlitCount = slitCount;
            if (matWaveCRT != null)
                matWaveCRT.SetFloat("_SlitCount", slitCount);
        }
    }

    public float SlitWidth
    {
        get => slitWidth;
        set
        {
            crtUpdateRequired |= value != slitWidth;
            slitWidth = value;
            //if (vectorDrawing != null)
            //    vectorDrawing.SetProgramVariable<float>("slitWidth", slitWidth);
            if (widthSlider != null && widthSlider.value != slitWidth)
                widthSlider.SetValueWithoutNotify(slitWidth);
            if (widthValue != null)
                widthValue.text = string.Format("{0:0.0}\nmm", slitWidth);
            if (particleSim != null)
                particleSim.SlitWidth = slitWidth * controlScale;
            if (matWaveCRT != null)
                matWaveCRT.SetFloat("_SlitWidth", slitWidth * waveMeshScale);
        }
    }

    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            crtUpdateRequired |= value != slitPitch;
            slitPitch = value;
            if (pitchSlider != null && pitchSlider.value != slitPitch)
                pitchSlider.SetValueWithoutNotify(slitPitch);
            if (pitchValue != null)
                pitchValue.text = string.Format("{0}\nmm", Mathf.RoundToInt(slitPitch));
            //if (vectorDrawing != null)
            // vectorDrawing.SetProgramVariable<float>("slitPitch", slitPitch);
            if (particleSim != null)
                particleSim.SlitPitch =  slitPitch * controlScale;
            if (matWaveCRT != null)
                matWaveCRT.SetFloat("_SlitPitch", slitPitch * waveMeshScale);
        }
    }

    public float SimScale
    {
        get => simScale;
        set
        {
            crtUpdateRequired |= simScale != value;
            simScale = value;
            if (scaleSlider != null && scaleSlider.value != simScale)
                scaleSlider.SetValueWithoutNotify(simScale);
            if ( scaleValue != null)
                scaleValue.text = string.Format("1:{0:0.0}", simScale);
            if (particleSim != null)
                particleSim.SimScale = simScale;
            if (matWaveCRT != null)
                matWaveCRT.SetFloat("_Scale", simScale);
        }
    }

    private float MinLambda
    {
        get => minLambda;
        set
        {
            minLambda = Mathf.Max(value, 10); // !! Hard coded to 10mm
            if (particleSim != null)
                particleSim.MaxParticleK = 1 / (minLambda*controlScale);
        }
    }

    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = Mathf.Clamp(value, minLambda,maxLambda);
            if (lambdaSlider != null && lambdaSlider.value != lambda)
                lambdaSlider.SetValueWithoutNotify(lambda);
            SetColour();
            updateLambda();
            updateMomentum();
        }
    }

    private void updateMomentum()
    {
        momentum = 1/(lambda*controlScale);
        if (lblMomentum != null)
            lblMomentum.text = string.Format("p={0:0.0}", momentum);
        if (particleSim != null)
        {
            particleSim.ParticleK = momentum;
            particleSim.DisplayColor =  displayColour;
        }
    }

    bool started = false;
    float timeCount = 1;
    private void Update()
    {
        timeCount -= Time.deltaTime;
        if (timeCount > 0)
            return;
        timeCount += 0.0333f;
        if (!crtUpdateRequired)
            return;
        crtUpdateRequired = false;
        if (waveCRT != null)
            waveCRT.Update(1);
        if (started)
            return;
        if (waveDisplayUI != null)
            waveDisplayUI.FlowColour = displayColour;
    }

    private void Awake()
    {
        if (waveCRT != null)
        {
            matWaveCRT = waveCRT.material;
            waveCrtSizePx = new Vector2Int(waveCRT.width, waveCRT.height);
        }
        if (widthSlider != null)
        {
            widthSlider.maxValue = 5;
            widthSlider.maxValue = 75;
            widthSlider.SetValueWithoutNotify(slitWidth);
        }
        if (pitchSlider != null)
        {
            pitchSlider.minValue = 40;
            pitchSlider.maxValue = 200;
            pitchSlider.SetValueWithoutNotify(slitPitch);
        }
        if (lambdaSlider != null)
        {
            lambdaSlider.minValue = minLambda;
            lambdaSlider.maxValue = maxLambda;
            lambdaSlider.SetValueWithoutNotify(lambda);
        }
        if (scaleSlider != null)
        {
            scaleSlider.minValue = 1;
            scaleSlider.maxValue = 5;
            scaleSlider.SetValueWithoutNotify(simScale);
        }
    }
    void Start()
    {
        waveMeshScale = controlScale * waveCrtSizePx.y /(waveCrtDims.y > 0 ? waveCrtDims.y : 1);
    
        MinLambda = lambdaSlider != null ? lambdaSlider.minValue : minLambda;
        initSimulations();
        SlitCount = slitCount;
        SlitWidth = slitWidth;
        SlitPitch = slitPitch;
        Lambda = lambda;
        SimScale = simScale;
        SetColour();
    }
}
