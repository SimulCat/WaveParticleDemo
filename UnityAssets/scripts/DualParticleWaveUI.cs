using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WaveParticleSim
{
    public class DualParticleWaveUI : MonoBehaviour
    {
        [Header("Demo Handlers")]
        [SerializeField]
        CRTQuantumUI particleSim;
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
        [SerializeField, Tooltip("Scales control settings in mm to lengths in metres")]
        private float mmToMetres = 0.001f;
        [SerializeField]
        private int slitCount;
        [SerializeField]
        private float slitWidth;
        [SerializeField]
        private float slitPitch;
        [SerializeField, Range(1, 5)]
        private float simScale;
        [SerializeField]
        private float particleMomentum;
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
        [SerializeField] private Slider momentumSlider;
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
        [SerializeField] Vector2 minMaxMomentum = new Vector2(1, 10);
        //[SerializeField]
        Material matWaveCRT;
        //[SerializeField]
        private float waveMeshScale = 1; // Scales UI control values for wavelength and grating dimensions to CRT gridpoints units

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
            float frac = Mathf.InverseLerp(minMaxMomentum[0], minMaxMomentum[1], particleMomentum);
            Color dColour = QM.momentumColour(frac);
            DisplayColour = dColour;
        }

        private void updateLambda()
        {
            if (lblLambda != null)
                lblLambda.text = string.Format("λ={0:0.0}", lambda);
            if (matWaveCRT != null)
            {
                matWaveCRT.SetFloat("_Lambda", lambda * waveMeshScale);
                crtUpdateRequired = true;
            }
            //if (vectorDrawing != null)
            //    vectorDrawing.SetProgramVariable<float>("lambda", lambda);
            if (waveDisplayUI != null)
                waveDisplayUI.Frequency = waveSpeed / lambda;
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
            minMaxMomentum = new Vector2(1 / (maxLambda * mmToMetres), 1 / (minLambda * mmToMetres));
            if (particleSim != null)
            {
                particleSim.MaxParticleP = minMaxMomentum[1];
                particleSim.MinParticleP = minMaxMomentum[0];
                particleSim.SimScale = simScale;
            }
            if (matWaveCRT != null)
            {
                matWaveCRT.SetFloat("_SlitCount", slitCount);
                matWaveCRT.SetFloat("_SlitWidth", slitWidth * waveMeshScale);
                matWaveCRT.SetFloat("_SlitPitch", slitPitch * waveMeshScale);
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
                    widthValue.text = slitWidth < 10 ? string.Format("{0:0.0}mm", slitWidth) : string.Format("{0}mm", Mathf.RoundToInt(slitWidth));
                widthValue.text = string.Format("{0:0.0}mm", slitWidth);
                if (particleSim != null)
                    particleSim.SlitWidth = slitWidth * mmToMetres;
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
                    pitchValue.text = slitPitch < 10 ? string.Format("{0:0.0}mm", slitPitch) : string.Format("{0}mm", Mathf.RoundToInt(slitPitch));
                //if (vectorDrawing != null)
                // vectorDrawing.SetProgramVariable<float>("slitPitch", slitPitch);
                if (particleSim != null)
                    particleSim.SlitPitch = slitPitch * mmToMetres;
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
                if (scaleValue != null)
                    scaleValue.text = string.Format("1:{0:0.0}", simScale);
                if (particleSim != null)
                    particleSim.SimScale = simScale;
                if (matWaveCRT != null)
                    matWaveCRT.SetFloat("_Scale", simScale);
            }
        }

        public float MinLambda
        {
            get => minLambda;
            set
            {
                minLambda = Mathf.Max(value, 10); // !! Hard coded to 10mm
                if (particleSim != null)
                    particleSim.MaxParticleP = 1 / (minLambda * mmToMetres);
            }
        }
        public float ParticleMomentum
        {
            get => particleMomentum;
            set
            {
                particleMomentum = Mathf.Clamp(value, minMaxMomentum.x, minMaxMomentum.y);
                if (lblMomentum != null)
                    lblMomentum.text = string.Format("p={0:0.0}", particleMomentum);
                if (momentumSlider != null && momentumSlider.value != particleMomentum)
                    momentumSlider.SetValueWithoutNotify(particleMomentum);
                Lambda = 1f/(particleMomentum*mmToMetres);

                SetColour();
                if (particleSim != null)
                {
                    particleSim.ParticleP = particleMomentum;
                }
            }
        }


        public float Lambda
        {
            get => lambda;
            set
            {
                lambda = Mathf.Clamp(value, minLambda, maxLambda);
                updateLambda();
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
                widthSlider.minValue = 10;
                widthSlider.maxValue = 75;
                widthSlider.SetValueWithoutNotify(slitWidth);
            }
            if (pitchSlider != null)
            {
                pitchSlider.minValue = 40;
                pitchSlider.maxValue = 300;
                pitchSlider.SetValueWithoutNotify(slitPitch);
            }
            if (momentumSlider != null)
            {
                minMaxMomentum = new Vector2(1 / (maxLambda * mmToMetres), 1 / (minLambda * mmToMetres));
                particleMomentum = Mathf.Clamp(particleMomentum, minMaxMomentum[0], minMaxMomentum[1]);
                momentumSlider.minValue = minMaxMomentum[0];
                momentumSlider.maxValue = minMaxMomentum[1];
                momentumSlider.SetValueWithoutNotify(particleMomentum);
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
            if (momentumSlider != null)
            {
                momentumSlider.onValueChanged.AddListener((v) => ParticleMomentum = v);
            }
            {
                if (scaleSlider != null)
                    scaleSlider.onValueChanged.AddListener((v) => SimScale = v);
            }
            waveMeshScale = mmToMetres * waveCrtSizePx.y / (waveCrtDims.y > 0 ? waveCrtDims.y : 1);
            initSimulations();
            SlitCount = slitCount;
            SlitWidth = slitWidth;
            SlitPitch = slitPitch;
            ParticleMomentum = particleMomentum;
            SimScale = simScale;
            SetColour();
        }
    }
}

