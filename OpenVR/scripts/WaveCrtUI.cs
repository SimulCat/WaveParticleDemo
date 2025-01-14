using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class WaveCrtUI : MonoBehaviour
{
    [SerializeField, Tooltip("Custom Render texture")]
    private CustomRenderTexture simCRT;

    [SerializeField] private Slider lambdaSlider;
    [SerializeField] private TextMeshProUGUI lambdaValue;

    [SerializeField, Range(1, 100)] private float lambda = 24;

    [SerializeField] private Slider pitchSlider;
    [SerializeField] private TextMeshProUGUI pitchValue;

    [SerializeField] private Slider widthSlider;
    [SerializeField] private TextMeshProUGUI widthValue;

    [SerializeField, Range(20, 500)]
    float slitPitch = 250;

    [SerializeField, Range(2, 25)]
    private float slitWidth = 10;

    [SerializeField] private int numSources = 2;
    [SerializeField] TextMeshProUGUI lblSourceCount;

    [SerializeField] private Slider scaleSlider;
    [SerializeField] private TextMeshProUGUI scaleValue;
    [SerializeField, Range(1, 10)] private float simScale = 2;

    [Header("Serialized for monitoring in Editor")]
    [SerializeField]
    private Material matSimControl = null;
    [SerializeField]
    private bool crtUpdateNeeded = false;
    private bool iHaveCRT = false;

    private void updateGrating()
    {
        if (!iHaveCRT)
            return;
        matSimControl.SetFloat("_SlitPitch", slitPitch);
        crtUpdateNeeded |= iHaveCRT;
        if (numSources > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (numSources - 1) * slitPitch + slitWidth;
            matSimControl.SetFloat("_SlitCount", 1f);
            matSimControl.SetFloat("_SlitWidth", gratingWidth);
            return;
        }
        matSimControl.SetFloat("_SlitCount", numSources);
        matSimControl.SetFloat("_SlitWidth", slitWidth);
    }
    public int NumSources
    {
        get => numSources;
        set
        {
            if (value < 1)
                value = 1;
            if (value > 17)
                value = 17;
            numSources = value;
            updateGrating();
            if (lblSourceCount != null)
                lblSourceCount.text = numSources.ToString();
        }
    }
    
    public void incSources()
    {
        NumSources = numSources + 1;
    }

    public void decSources()
    {
        NumSources = numSources - 1;
    }



    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            if (matSimControl != null)
                matSimControl.SetFloat("_Lambda", lambda);
            if (lambdaSlider != null && lambdaSlider.value != value)
                lambdaSlider.SetValueWithoutNotify(lambda);
            if (lambdaValue != null)
                lambdaValue.text = string.Format("{0}mm", Mathf.RoundToInt(lambda));
            crtUpdateNeeded |= iHaveCRT;
        }
    }

    public void onLambda()
    {
        if (lambdaSlider != null)
            Lambda = lambdaSlider.value;
    }

    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            slitPitch = value;
            if (pitchSlider != null && pitchSlider.value != value)
                pitchSlider.SetValueWithoutNotify(slitPitch);
            if (pitchValue != null)
                pitchValue.text = string.Format("{0}\nmm", Mathf.RoundToInt(slitPitch));
            updateGrating();
        }
    }

    public void onSlitPitch()
    {
        if (pitchSlider != null)
            SlitPitch = pitchSlider.value;
    }
    public float SlitWidth
    {
        get => slitWidth;
        set
        {
            slitWidth = value;
            if (widthSlider != null && widthSlider.value != value)
                widthSlider.SetValueWithoutNotify(slitWidth);
            if (widthValue != null)
                widthValue.text = string.Format("{0:0.0}\nmm", slitWidth);
            updateGrating() ;
        }
    }

    public void onSlitWidth()
    {
        if (widthSlider != null)
        {
            SlitWidth = widthSlider.value;
            Debug.Log(gameObject.name + "onSlitWidth=" + SlitWidth);
        }
    }
    public float SimScale
    {
        get => simScale;
        set
        {
            simScale = value;
            if (matSimControl != null)
                matSimControl.SetFloat("_Scale", simScale);
            if (scaleSlider != null && scaleSlider.value != value)
                scaleSlider.SetValueWithoutNotify(simScale);
            if (scaleValue != null)
                scaleValue.text = string.Format("1:{0:0.0}", simScale);
            crtUpdateNeeded |= iHaveCRT;
        }
    }

    public void onSimScale()
    {
        if (scaleSlider != null)
            SimScale = scaleSlider.value;
    }

    void UpdateSimulation()
    {
        if (iHaveCRT)
            simCRT.Update(1);
        simCRT.Update(1);
        crtUpdateNeeded = false;
        //Debug.Log(gameObject.name + "UpdateSimulation()");
    }

    private void Update()
    {
        if (!iHaveCRT)
            return;
        if (crtUpdateNeeded)
        {
           UpdateSimulation();
        }
    }

    private void Awake()
    {
        if (simCRT != null)
        {
            iHaveCRT = true;
            if (matSimControl == null)
                matSimControl = simCRT.material;
        }
        if (pitchSlider != null)
        {
            pitchSlider.maxValue = 500;
            pitchSlider.minValue = 10;
        }
        if (widthSlider != null)
        {
            widthSlider.maxValue = 25;
            widthSlider.minValue = 1;
        }
    }
    void Start()
    {

        NumSources = numSources;
        SlitPitch = slitPitch;
        SlitWidth = slitWidth;
        Lambda = lambda;
        SimScale = simScale;
        crtUpdateNeeded |= iHaveCRT;
    }
}
