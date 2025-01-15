﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuantumScatter : MonoBehaviour
{
    [Header("Simulation Components")]
    [SerializeField,Tooltip("CRT to generate probability density")]
    private CustomRenderTexture probabilityCRT;
    [SerializeField,Tooltip("Simulation Panel Dimensions")]
    private Vector2 simSize = new Vector2(2.56f, 1.6f);
    [SerializeField] 
    private bool showProbability = true;
    [SerializeField]
    private float probVisPercent = 45f;
    [SerializeField]
    Vector2Int simPixels = new Vector2Int(1024, 640);
    
    [SerializeField]
    private string texName = "_MomentumMap";
    [SerializeField]
    MeshRenderer particleMeshRend = null;
    [SerializeField]
    private float visibility = 1;


    [Header("Scattering Configuration")]
    
    [SerializeField, Tooltip("Distribution Points")]
    private int pointsWide = 256;
    //[SerializeField, Tooltip("Planck Value for simulation; lambda=h/p"),FieldChangeCallback(nameof(PlanckSim))]
    //public float planckSim = 12;

    [Header("Grating Configuration & Scale")]
    [SerializeField] 
    private float gratingOffset = 0;
    [SerializeField,Range(1,17)]
    private int slitCount = 2;          // SlitCount("Num Sources", float)
    [SerializeField]
    private float slitPitch = 45f;        // "Slit Pitch" millimetre
    [SerializeField]
    private float slitWidth = 12f;        // "Slit Width" millimetres
// Pulsed particles and speed range
    [SerializeField]
    private bool pulseParticles = false;
    [SerializeField, Range(0.01f, 1.5f)]
    private float pulseWidth = 1f;        // particle Pulse width
    [SerializeField, Range(0,50)]
    private float speedRange = 10f;        // Speed Range Percent

    [SerializeField, Range(1, 10)]
    private float simScale;
    [SerializeField]
    public Color displayColor = Color.cyan;
    [SerializeField]
    public float maxParticleK = 10;
    [SerializeField]
    private float minParticleK = 1;

    private float Visibility
    {
        get => visibility;
        set
        {
            visibility = Mathf.Clamp01(value);
            if (matParticleFlow != null)
            {
                matParticleFlow.SetFloat("_Visibility", visibility);
            }
            reviewProbVisibility();
        }
    }

    public float MaxParticleK 
    {   get=>maxParticleK; 
        set 
        { 
            maxParticleK = value; 
        } 
    }
    public float MinParticleK { get => minParticleK; set => minParticleK = value; }
    [SerializeField]
    private float particleK = 1;

    [Header("UI Elements")]
    [SerializeField] Toggle togPlay;
    [SerializeField] Toggle togPause;
    [SerializeField] Toggle togShowHide;
    [SerializeField] Toggle togProbability;
    [SerializeField] Toggle togPulseParticles;
    [SerializeField] Slider probVizSlider;
    [SerializeField] TextMeshProUGUI probVizValue; 
    [SerializeField] Slider pulseWidthSlider;
    [SerializeField] TextMeshProUGUI pulseWidthValue;
    [SerializeField] Slider speedRangeSlider;
    [SerializeField] TextMeshProUGUI SpeedRangeValue;

    [Header("For tracking in Editor")]
    //[SerializeField, Tooltip("Shown for editor reference, loaded at Start")]
    private Material matProbabilitySim = null;
    //[SerializeField]
    private Material matParticleFlow = null;

    //[SerializeField]
    private float shaderPauseTime = 0;
    //[SerializeField]
    private float shaderBaseTime = 0;
    [SerializeField]
    private bool shaderPlaying = false;

    private float prevVisibility = -1;
    private void reviewProbVisibility()
    {
        if (matProbabilitySim == null) 
            return;
        float targetViz = visibility * (showProbability ? ProbVisPercent/10 : 0);
        if (targetViz == prevVisibility)
            return;
        prevVisibility = targetViz;
        matProbabilitySim.SetFloat("_Brightness", targetViz);
        crtUpdateRequired = true;
    }

    private void reviewPulse()
    {
        if (matParticleFlow == null)
            return;
        float width = pulseParticles ? pulseWidth : -1f;
        matParticleFlow.SetFloat("_PulseWidth", width);
        if (togPulseParticles != null && togPulseParticles.isOn != pulseParticles)
            togPulseParticles.SetIsOnWithoutNotify(pulseParticles);

    }
    public bool ShowProbability
    {
        get=> showProbability;
        set
        {
            bool chg = showProbability != value;
            showProbability = value;
            if (togProbability != null && togProbability.isOn != showProbability)
                togProbability.SetIsOnWithoutNotify(showProbability);
            if (probVizSlider != null)
                probVizSlider.interactable = showProbability;
            if (chg)
                reviewProbVisibility();
        }
    }

    public bool PulseParticles
    {
        get => pulseParticles;
        set
        {
            bool chg = pulseParticles != value;
            pulseParticles = value;
            if (togPulseParticles != null && togPulseParticles.isOn != value)
                togPulseParticles.SetIsOnWithoutNotify(pulseParticles);
            if (chg) 
                reviewPulse();
        }
    }

    public float ProbVisPercent
    {
        get=> probVisPercent;
        set
        {
            //Debug.Log("ProbvizPct :"+ value);
            if (probVizSlider != null && probVizSlider.value != value)
                probVizSlider.SetValueWithoutNotify(value);
            if (probVizValue != null)
                probVizValue.text = string.Format("{0}%", Mathf.RoundToInt(value));
            probVisPercent = value;
            reviewProbVisibility();
        }
    }


    /*
     * Synced Properties
     */
    private void updatePlayPauseStop()
    {
        switch (particlePlayState)
        { 
            case 0:
                if (togPause != null && !togPause.isOn)
                    togPause.SetIsOnWithoutNotify(true);
                break;
            case 1:
                if (togPlay != null && !togPlay.isOn)
                    togPlay.SetIsOnWithoutNotify(true);
                break;
            default:
                if (togShowHide != null && !togShowHide.isOn)
                    togShowHide.SetIsOnWithoutNotify(true);
                break;
        }
    }

    [SerializeField] int particlePlayState = 1;
    public int ParticlePlayState
    {
        get => particlePlayState;
        set
        {
            particlePlayState = value;
            setParticlePlay(particlePlayState);
            updatePlayPauseStop();
        }
    }

    //[SerializeField]
    bool crtUpdateRequired = false;
   // [SerializeField]
    bool gratingUpdateRequired = false;
   // [SerializeField]
    float simPixelScale = 1;

    private void setGratingParams(Material mat)
    {
        mat.SetFloat("_SlitCount", 1f*slitCount);
        mat.SetFloat("_SlitWidth", slitWidth * simPixelScale);
        mat.SetFloat("_SlitPitch", slitPitch * simPixelScale);
        mat.SetFloat("_Scale", simScale);
        mat.SetFloat("_GratingOffset", gratingOffset);
    }
    private void setParticleParams(Material mat)
    {
        mat.SetFloat("_SlitCount", 1f*slitCount);
        mat.SetFloat("_SlitWidth", slitWidth);
        mat.SetFloat("_SlitPitch", slitPitch);
        mat.SetFloat("_Scale", simScale);
        mat.SetFloat("_GratingOffset", gratingOffset);
    }

    private void initParticlePlay()
    {
        shaderBaseTime = 0;
        shaderPauseTime = 0;
        matParticleFlow.SetFloat("_PauseTime", 0f);
        matParticleFlow.SetFloat("_BaseTime", shaderBaseTime);
        matParticleFlow.SetFloat("_Play", 1f);
        shaderPlaying = true;
        //Debug.Log("Init");
    }
    private void setParticlePlay(int playState)
    {
        if (matParticleFlow == null)
            return;
        particleMeshRend.enabled = playState >= 0;
        switch (playState)
        {
            case 1:
                if (!shaderPlaying)
                {
                    shaderBaseTime += Time.timeSinceLevelLoad - shaderPauseTime;
                    matParticleFlow.SetFloat("_BaseTime", shaderBaseTime);
                    matParticleFlow.SetFloat("_Play", 1f);
                    shaderPlaying = true;
                    //Debug.Log("Play");
                }
                break;
            case 0:
                if (shaderPlaying)
                {
                    shaderPauseTime = Time.timeSinceLevelLoad;
                    matParticleFlow.SetFloat("_PauseTime", shaderPauseTime);
                    matParticleFlow.SetFloat("_Play", 0f);
                    shaderPlaying = false;
                    //Debug.Log("Pause");
                }
                break;
            default: 
                return;
        }
    }

    public void SetGrating(int numSlits, float widthSlit, float pitchSlits, float momentumMax)
    {
        //Debug.Log(string.Format("{0} SetGrating: #slit1={1} width={2} pitch={3}", gameObject.name,  numSlits, widthSlit, pitchSlits));

        bool isChanged = numSlits != slitCount || widthSlit != slitWidth || pitchSlits != slitPitch;
        isChanged |= maxParticleK != momentumMax;
        slitCount = numSlits;
        maxParticleK = momentumMax;
        slitWidth = widthSlit;
        slitPitch = pitchSlits;
        gratingUpdateRequired = isChanged;
        if (isChanged)
        {
            if (matProbabilitySim != null) setGratingParams(matProbabilitySim);
            if (matParticleFlow != null) setParticleParams(matParticleFlow);
        }
    }

    
    private float GratingOffset
    {
        get=>gratingOffset;
        set
        {
            gratingOffset = value;
            //Debug.Log("GratingOffset=" + value.ToString());
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_GratingOffset", gratingOffset*simPixelScale);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_GratingOffset", gratingOffset);
        }
    }
    
    public float SimScale
    {
        get => simScale;
        set
        {
            if (value != simScale)
                crtUpdateRequired = true;
            simScale = value;
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_Scale", simScale);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_Scale", simScale);
        }
    }

    float beamWidth = 1;
    private void UpdatebeamWidth()
    {
        beamWidth = Mathf.Max(slitCount-1,0)* slitPitch + slitWidth*1.3f;
        if (matProbabilitySim != null)
            matProbabilitySim.SetFloat("_BeamWidth", beamWidth * simPixelScale);
        if (matParticleFlow != null)
            matParticleFlow.SetFloat("_BeamWidth", beamWidth);
    }
    public int SlitCount
    {
        get => slitCount;
        set
        {
            if (value != slitCount)
            {
                gratingUpdateRequired = true;
                crtUpdateRequired = true;
            }
            slitCount = value;
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_SlitCount", 1f*slitCount);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_SlitCount", 1f* slitCount);
            UpdatebeamWidth();
        }
    }
    public float SlitWidth
    {
        get => slitWidth;
        set
        {
            if (value != slitWidth)
            {
                gratingUpdateRequired = true;
                crtUpdateRequired = true;
            }
            slitWidth = value;
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_SlitWidth", slitWidth * simPixelScale);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_SlitWidth", slitWidth);
            UpdatebeamWidth();
        }
    }

    public float PulseWidth
    {
        get => pulseWidth;
        set
        {
            value = Mathf.Clamp(value, 0.1f, 2f);
            bool chg = value != pulseWidth;
            if (pulseWidthSlider != null && pulseWidthSlider.value != value)
                pulseWidthSlider.SetValueWithoutNotify(value);
            if (pulseWidthValue != null)
                pulseWidthValue.text = string.Format("{0:0.0}s", value);
            pulseWidth = value;
            if (chg)
                reviewPulse();
        }
    }

    public float SpeedRange
    {
        get => speedRange;
        set
        {
           speedRange = Mathf.Clamp(value,0,50);
            if (speedRangeSlider != null && speedRangeSlider.value != speedRange)
                speedRangeSlider.SetValueWithoutNotify(value);
            if (SpeedRangeValue != null)
                SpeedRangeValue.text = string.Format("{0}%", Mathf.RoundToInt(speedRange));
            if (matParticleFlow == null)
                matParticleFlow.SetFloat("_SpeedRange", value / 100f);
        }
    }

    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            if (value != slitPitch)
            {
                gratingUpdateRequired = true;
                crtUpdateRequired = true;
            }
            slitPitch = value;
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_SlitPitch", slitPitch * simPixelScale);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_SlitPitch", slitPitch);
            UpdatebeamWidth();
        }
    }

    public float ParticleK
    {
        get => particleK;
        set
        {
            crtUpdateRequired |= value != particleK;
            particleK = value;
            //float particleP = particleK / planckSim;
            if (matProbabilitySim != null)
                matProbabilitySim.SetFloat("_ParticleP", particleK);
            if (matParticleFlow != null)
                matParticleFlow.SetFloat("_ParticleP", particleK);
        }
    }

    public Color DisplayColor
    {
        get => displayColor;
        set
        {
           // Debug.Log(gameObject.name + ": displayColour->" + value.ToString());
            displayColor = value;
            if (matProbabilitySim != null)
                matProbabilitySim.SetColor("_Color", displayColor);
            if (matParticleFlow != null)
                matParticleFlow.SetColor("_Color", displayColor);
        }
    }

    private bool hasMaterialWithProperty(Material theMaterial, string thePropertyName)
    {
        return (theMaterial != null) && theMaterial.HasProperty(thePropertyName); 
    }

    private float sampleDistribution(float spatialK)
    {

        float slitPhase = spatialK * slitWidth;

        float apertureProbSq = Mathf.Abs(slitPhase) > 0.000001f ? Mathf.Sin(slitPhase) / slitPhase : 1.0f;
        apertureProbSq *= apertureProbSq;
        float multiSlitProbSq = 1f;
        if (slitCount > 1)
        {
            float gratingPhase = spatialK * slitPitch;
            if (slitCount == 2)
                multiSlitProbSq = Mathf.Cos(gratingPhase) * 2;
            else
            {
                float sinGrPhase = Mathf.Sin(gratingPhase);
                multiSlitProbSq = (Mathf.Abs(sinGrPhase) < 0.000001f) ? slitCount : Mathf.Sin(slitCount * gratingPhase) / sinGrPhase;
            }
            multiSlitProbSq *= multiSlitProbSq;
        }
        return multiSlitProbSq * apertureProbSq;
    }
  // [SerializeField]
    private float[] gratingFourierSq;
   //[SerializeField]
    private float[] probIntegral;
   //[SerializeField]
    private float[] weightedLookup;
    private void GenerateSamples()
    {
        if (gratingFourierSq == null || gratingFourierSq.Length < pointsWide)
        {
            gratingFourierSq = new float[pointsWide];
            probIntegral = new float[pointsWide+1];
        }
        float impulse;
        float prob;
        float pi_h = Mathf.PI;// / planckSim;
        float probIntegralSum = 0;
        for (int i = 0; i < pointsWide; i++)
        {
            impulse = (maxParticleK * i) / pointsWide;
            prob = sampleDistribution(impulse * pi_h);
            gratingFourierSq[i] = prob;
            probIntegral[i] = probIntegralSum;
            probIntegralSum += prob;
        }
        probIntegral[pointsWide] = probIntegralSum;
        // Scale (Normalize?) Integral to Width of Distribution for building inverse lookup;
        float normScale = (pointsWide-1) / probIntegral[pointsWide-1];
        for (int nPoint = 0; nPoint <= pointsWide; nPoint++)
            probIntegral[nPoint] *= normScale;
        //probIntegral[pointsWide] = pointsWide;
    }

    private void GenerateReverseLookup()
    {
        if (weightedLookup == null || weightedLookup.Length < pointsWide)
            weightedLookup = new float[pointsWide];
        // Scale prob distribution to be 0 to pointsWide at max;
        int indexAbove = 0;
        int indexBelow;
        float vmin;
        float vmax = 0;
        float frac;
        float val;
        int lim = pointsWide-1;
        float norm = maxParticleK / lim; 
        for (int i = 0; i <= lim; i++)
        {
            while ((vmax <= i) && (indexAbove <= lim))
            {
                indexAbove++;
                vmax = probIntegral[indexAbove];
            }
            vmin = vmax; indexBelow = indexAbove;
            while ((indexBelow > 0) && (vmin > i))
            {
                indexBelow--;
                vmin = probIntegral[indexBelow];
            }
            //Debug.Log(string.Format("i:{0}, ixAbove{1}, vmax:{2}, ixBelow:{3}, vmin{4}",i, indexAbove, vmax, indexBelow, vmin));
            if (indexBelow >= indexAbove)
                val = vmax;
            else
            {
                frac = Mathf.InverseLerp(vmin, vmax, i);
                val = Mathf.Lerp(indexBelow, indexAbove, frac);
            }
            weightedLookup[i] = val * norm;///lim;
        }
    }

    public bool CreateTextures()
    {
        simPixelScale = simPixels.y / simSize.y;

        GenerateSamples();

        Color[] texData = new Color[pointsWide + pointsWide];

        if (matProbabilitySim != null)
        {
            var tex = new Texture2D(pointsWide * 2, 1, TextureFormat.RGBAFloat, 0, true);

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float impulse;
            for (int i = 0; i < pointsWide; i++)
            {
                impulse = (maxParticleK * i) / pointsWide;

                float sample = gratingFourierSq[i];
                float integral = probIntegral[i];
                texData[pointsWide + i] = new Color(sample, integral, impulse, 1f);
                texData[pointsWide - i] = new Color(sample, -integral, -impulse, 1f);
            }
            matProbabilitySim.SetFloat("_MapMaxP", maxParticleK); // "Map max momentum", float ) = 1
            matProbabilitySim.SetFloat("_MapMaxI", probIntegral[pointsWide - 1]); // "Map Summed probability", float ) = 1
            texData[0] = new Color(0, -probIntegral[pointsWide-1], -1, 1f);

            // Normalize
            //float total = texData[pointsWide-1].g;
            //for (int i = 0;i < pointsWide; i++)
            //    texData[i].g /= total;
            tex.SetPixels(0, 0, pointsWide * 2, 1, texData, 0);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            matProbabilitySim.SetTexture(texName, tex);
        }
        if (matParticleFlow != null)
        {
            GenerateReverseLookup();
            texData = new Color[pointsWide];
            float norm = 1f/(pointsWide - 1);
            for (int i = 0; i < pointsWide; i++)
            {
                float integral = probIntegral[i] * norm;
                float sample = gratingFourierSq[i];
                float reverse = weightedLookup[i];
                texData[i] = new Color(sample, integral, reverse, 1f);
            }
            var tex = new Texture2D(pointsWide, 1, TextureFormat.RGBAFloat, 0, true);
            tex.SetPixels(0, 0, pointsWide, 1, texData, 0);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            matParticleFlow.SetTexture(texName, tex);

            matParticleFlow.SetFloat("_MapMaxP", maxParticleK); // "Map max momentum", float ) = 1
        }
        //Debug.Log(" Created Texture: [" + texName + "]");
        crtUpdateRequired = true;
        return true;
    }

    /* UI Stuff
     * 
     */
    // play/pause reset particle events
    public void simHide()
    {
        if (togShowHide != null && togShowHide.isOn && particlePlayState >= 0)
        {
            ParticlePlayState = -1;
        }
    }
    public void simPlay()
    {
        if (togPlay != null && togPlay.isOn && particlePlayState <= 0)
        {
            ParticlePlayState = 1;
        }
    }

    public void simPause()
    {
        if (togPause != null && togPause.isOn && particlePlayState != 0)
        {
            ParticlePlayState = 0;
        }
    }

    public void showProb()
    {
        if ((togProbability != null) && togProbability.isOn != showProbability)
            ShowProbability = !showProbability;
    }

    public void togPulse()
    {
        if (togPulseParticles != null && togPulseParticles.isOn != pulseParticles)
            PulseParticles = !pulseParticles;
    }
    /*
     * Update and Start
     */
    private float updateTimer = 1;
    private bool init = false;
    private void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer > 0)
            return;
        if (!init)
        {
            init = true;
            initParticlePlay();
        }
        if (gratingUpdateRequired)
        {
            CreateTextures();
            crtUpdateRequired = true;
            gratingUpdateRequired = false;
            updateTimer += 0.05f;
        }
        else
            updateTimer += 0.01f;
        if (crtUpdateRequired)
        {
            crtUpdateRequired = false;
            if (probabilityCRT != null)
                probabilityCRT.Update(1);
        }
    }

    void Awake()
    {
        simPixelScale = simPixels.y / simSize.y;
        if (probabilityCRT != null)
            matProbabilitySim = probabilityCRT.material;
        if (!hasMaterialWithProperty(matProbabilitySim, texName))
            matProbabilitySim = null;
        if (particleMeshRend != null)
        {
            matParticleFlow = particleMeshRend.material;
        }
        if (!hasMaterialWithProperty(matParticleFlow, texName))
            matParticleFlow = null;
        if (speedRangeSlider != null)
        {
            speedRangeSlider.minValue = 0;
            speedRangeSlider.maxValue = 50;
            speedRangeSlider.SetValueWithoutNotify(speedRange);
        }
        if (pulseWidthSlider != null)
        {
            pulseWidthSlider.minValue = 0.1f;
            pulseWidthSlider.maxValue = 1.5f;
            pulseWidthSlider.SetValueWithoutNotify(pulseWidth);
        }
        if (probVizSlider != null)
            probVizSlider.SetValueWithoutNotify(probVisPercent);
    }
    void Start()
    {
        //Debug.Log("BScatter Start");
        ShowProbability = showProbability;
        SlitCount = slitCount;
        SlitWidth = slitWidth;
        SlitPitch = slitPitch;
        SimScale = simScale;
        SpeedRange = speedRange;
        PulseParticles = pulseParticles;
        PulseWidth = pulseWidth;
        Visibility = visibility;
        ProbVisPercent = probVisPercent;
        reviewPulse();
        GratingOffset = gratingOffset;
        ParticleK = particleK;
    }
}
