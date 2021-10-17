﻿using UnityEngine;
using System.Collections;
using UnityEngine.VFX;

public class Fluid2D : MonoBehaviour {

    public VisualEffect vfx;
 //   public VisualEffect[] waterfalls;

	public int BufferSizeWidth  = 512;
	public int BufferSizeHeight = 512;

    public Transform[] TrackingObjects;
    public Transform[] Last_TrackingObjects;
    public Transform origin;

	
	[Range (0, 50)]
	public int SolverIterations = 50;
 public   Vector4[] Pforces;
  public  Vector4[] LastPforces;
    public Shader AdvectShader;
	public Shader DivergenceShader;
	public Shader PressureSolveShader;
	public Shader PressureGradientSubstractShader;
	public Shader ApplyForceShader;
	public Shader UpdateDyeShader;

	private Material _advectMat;
	private Material _divergenceMat;
	private Material _pressureSolveMat;
	private Material _pressureGradientSubtractMat;
	private Material _applyForceMat;
	private Material _updateDyeMat;

    public RenderTexture[] _velocityBuffer;
    public RenderTexture[] _pressureBuffer;
    public RenderTexture   _divergenceBuffer;
    public RenderTexture[] _dyeBuffer;

	private Vector2 _invResolution;
	//private float   _aspectRatio;
	private float   _RDX;
	
	[Range (0, 1024)]
	public int GUITextureSize = 320;

	public bool IsLeftMouseButtonDown = false;
	private Vector2 _currentMousePosition;
	private Vector2 _previousMousePosition;

	public RenderTexture GetFluidTex () {
		if (_dyeBuffer != null && _dyeBuffer.Length > 0) {
			return _dyeBuffer [0];
		} else {
			return null;
		}
	}

	public RenderTexture GetFlowVelocityFieldTex () {
		if (_velocityBuffer != null && _velocityBuffer.Length > 0) {
			return _velocityBuffer [0];
		} else {
			return null;
		}
	}

	void Start () {

		Setup ();


        Pforces=new Vector4[1000];
      LastPforces= new Vector4[1000];

    }
	
	void Update () {
/*
		if (Input.GetMouseButtonDown (0)) {
			IsLeftMouseButtonDown = true;
		}

		if (Input.GetMouseButtonUp (0)) {
			IsLeftMouseButtonDown = false;
		}
		
		if (Input.GetKeyUp ("r")) {
			_resetBuffers ();
		}
		*/
		Step (Time.deltaTime * .5f);


    vfx.SetTexture("VelMap", _velocityBuffer[0]);
     //   waterfall2.SetTexture("Force", _velocityBuffer[0]);
/*
        foreach (var w in waterfalls) { 

        w.SetTexture("FluidMap", _velocityBuffer[0]);
        }*/
    }
	
	void OnDestroy () {
		
		_destroyBuffers ();
		_destroyMaterials ();
		
	}


	public void Setup () {

		_RDX = 1.0f / (BufferSizeWidth * 1.0f);
		_invResolution = new Vector2 (1.0f / (BufferSizeWidth * 1.0f), 1.0f / (BufferSizeHeight * 1.0f));

		_createBuffers ();
		_resetBuffers ();
		_createMaterials ();

	}

	public void Step (float dt_) {

		//_aspectRatio = Screen.width / (Screen.height * 1.0f);
		Shader.SetGlobalFloat ("_Fluid2D_AspectRatio", 1.0f);

//		Vector3 mp = Input.mousePosition;
		//mp.x *= _aspectRatio;
	//_currentMousePosition = Camera.main.ScreenToViewportPoint (mp);

        Pforces = UpdatePosiiton();

        _advect (ref _velocityBuffer, dt_);
		_applyForces (dt_);
		_computeDivergence ();
		_solvePressure ();
		_subtractPressureGradient ();
         _updateDye (dt_);
		_advect (ref _dyeBuffer, dt_);


        LastPforces = Pforces;

      //  _previousMousePosition = _currentMousePosition;
	}

	void _advect (ref RenderTexture[] targetBuffer_, float dt_) {

		_advectMat.SetFloat ("_Dt", dt_);
		_advectMat.SetFloat ("_RDX", _RDX);
		_advectMat.SetVector  ("_Invresolution", _invResolution);
		_advectMat.SetTexture ("_Target", targetBuffer_ [0]);
		_advectMat.SetTexture ("_Velocity", _velocityBuffer [0]);
		Graphics.Blit (null, targetBuffer_ [1], _advectMat);
		_swapBuffer (targetBuffer_);
	}

	void _applyForces (float dt_) {
		if (_applyForceMat == null)
			return;
		//set uniforms
		_applyForceMat.SetTexture ("_Velocity", _velocityBuffer [0]);
		_applyForceMat.SetFloat ("_Dt", dt_);
		_applyForceMat.SetFloat ("_Dx", BufferSizeWidth);

		_applyForceMat.SetInt    ("_IsMouseDown",       IsLeftMouseButtonDown ? 1 : 0);

        _applyForceMat.SetVectorArray("_Pforce",Pforces);
        _applyForceMat.SetVectorArray("_Lastforce",LastPforces);
      

      //  _applyForceMat.SetVector ("_MouseClipSpace",     _currentMousePosition);
	//	_applyForceMat.SetVector ("_LastMouseClipSpace", _previousMousePosition);
	
		//render
		Graphics.Blit (null, _velocityBuffer [1], _applyForceMat);
		_swapBuffer (_velocityBuffer);

	}

	void _computeDivergence () {

		_divergenceMat.SetTexture ("_Velocity", _velocityBuffer [0]);

		_divergenceMat.SetFloat   ("_HalfRDX", 0.5f * _RDX);
		_divergenceMat.SetVector  ("_Invresolution", _invResolution);
		Graphics.Blit (null, _divergenceBuffer, _divergenceMat);
	}

	void _solvePressure () {

		_pressureSolveMat.SetTexture ("_Divergence", _divergenceBuffer);
		_pressureSolveMat.SetFloat   ("_Alpha", -(BufferSizeWidth * BufferSizeWidth));
		_pressureSolveMat.SetVector  ("_Invresolution", _invResolution);

		for (int i = 0; i < SolverIterations; i++) {
			_pressureSolveMat.SetTexture ("_Pressure", _pressureBuffer [0]);
			Graphics.Blit (null, _pressureBuffer [1], _pressureSolveMat);
			_swapBuffer (_pressureBuffer);
		}
	}

	void _subtractPressureGradient () {

		_pressureGradientSubtractMat.SetTexture ("_Pressure", _pressureBuffer [0]);
		_pressureGradientSubtractMat.SetTexture ("_Velocity", _velocityBuffer [0]);
		_pressureGradientSubtractMat.SetFloat   ("_HalfRDX", 0.5f * _RDX);
		_pressureGradientSubtractMat.SetVector  ("_Invresolution", _invResolution);
		Graphics.Blit (null, _velocityBuffer [1], _pressureGradientSubtractMat);

		_swapBuffer (_velocityBuffer);
	}

	void _updateDye (float dt_) {

		if(_updateDyeMat == null) return;

		_updateDyeMat.SetFloat   ("_Dt", dt_);
		_updateDyeMat.SetTexture ("_Dye", _dyeBuffer [0]);



        _updateDyeMat.SetInt     ("_IsMouseDown", IsLeftMouseButtonDown ? 1 : 0);
		//_updateDyeMat.SetVector  ("_MouseClipSpace", _currentMousePosition);
        //_updateDyeMat.SetVector  ("_LastMouseClipSpace", _previousMousePosition);





        Graphics.Blit (null, _dyeBuffer [1], _updateDyeMat);
		_swapBuffer (_dyeBuffer);

	}

	void _createBuffers () {
		
		_createBuffer (ref _velocityBuffer,   BufferSizeWidth, BufferSizeHeight);
		_createBuffer (ref _pressureBuffer,   BufferSizeWidth, BufferSizeHeight);
		_createBuffer (ref _divergenceBuffer, BufferSizeWidth, BufferSizeHeight);
		_createBuffer (ref _dyeBuffer,        BufferSizeWidth, BufferSizeHeight);
	}

	public void _resetBuffers () {
		
		_resetBuffer (ref _velocityBuffer);
		_resetBuffer (ref _pressureBuffer);
		_resetBuffer (ref _divergenceBuffer);
		_resetBuffer (ref _dyeBuffer);
		
	}

	void _destroyBuffers () {
		
		_destroyBuffer (ref _velocityBuffer);
		_destroyBuffer (ref _pressureBuffer);
		_destroyBuffer (ref _divergenceBuffer);
		_destroyBuffer (ref _dyeBuffer);
		
	}

	void _createMaterials () {
		
		_createMaterial (ref _advectMat,                   AdvectShader);
		_createMaterial (ref _divergenceMat,               DivergenceShader);
		_createMaterial (ref _pressureSolveMat,            PressureSolveShader);
		_createMaterial (ref _pressureGradientSubtractMat, PressureGradientSubstractShader);
		_createMaterial (ref _updateDyeMat,                UpdateDyeShader);
		_createMaterial (ref _applyForceMat,               ApplyForceShader);
	}

	void _destroyMaterials () {
		
		_destroyMaterial (ref _advectMat);
		_destroyMaterial (ref _divergenceMat);
		_destroyMaterial (ref _pressureSolveMat);
		_destroyMaterial (ref _pressureGradientSubtractMat);
		_destroyMaterial (ref _updateDyeMat);
		_destroyMaterial (ref _applyForceMat);

	}

	void _createBuffer (ref RenderTexture[] rt_, int bufferWidth_, int bufferHeight_) {
		
		rt_ = new RenderTexture[2];
		rt_ [0] = new RenderTexture (bufferWidth_, bufferHeight_, 0, RenderTextureFormat.ARGBHalf);
		rt_ [0].filterMode = FilterMode.Bilinear;
		rt_ [0].wrapMode   = TextureWrapMode.Clamp;
		rt_ [0].hideFlags  = HideFlags.DontSave;
		rt_ [0].Create ();
		rt_ [1] = new RenderTexture (bufferWidth_, bufferHeight_, 0, RenderTextureFormat.ARGBHalf);
		rt_ [1].filterMode = FilterMode.Bilinear;
		rt_ [1].wrapMode   = TextureWrapMode.Clamp;
		rt_ [1].hideFlags  = HideFlags.DontSave;
		rt_ [1].Create ();
		
	}

	void _createBuffer (ref RenderTexture rt_, int bufferWidth_, int bufferHeight_) {

		rt_ = new RenderTexture (bufferWidth_, bufferHeight_, 0, RenderTextureFormat.ARGBHalf);
		rt_.filterMode = FilterMode.Bilinear;
		rt_.wrapMode   = TextureWrapMode.Clamp;
		rt_.hideFlags  = HideFlags.DontSave;
		rt_.Create ();
	
	}

	void _resetBuffer (ref RenderTexture[] rt_) {
		
		Graphics.SetRenderTarget (rt_ [0]);
		GL.Clear (false, true, Color.black);
		Graphics.SetRenderTarget (null);
		
		Graphics.SetRenderTarget (rt_ [1]);
		GL.Clear (false, true, Color.black);
		Graphics.SetRenderTarget (null);
		
	}

	void _resetBuffer (ref RenderTexture rt_) {
		
		Graphics.SetRenderTarget (rt_);
		GL.Clear (false, true, Color.black);
		Graphics.SetRenderTarget (null);

	}

	void _destroyBuffer (ref RenderTexture[] buffer_) {
		
		if (buffer_ != null && buffer_.Length > 0) {
			for (int i = 0; i < buffer_.Length; i++) {
				DestroyImmediate (buffer_ [i]);
			}
		}
		
	}

	void _destroyBuffer (ref RenderTexture buffer_) {
		
		if (buffer_ != null) {
			DestroyImmediate (buffer_);
		}
		
	}
	
	void _swapBuffer (RenderTexture[] buffer_) {
		
		RenderTexture temp = buffer_ [0];
		buffer_ [0] = buffer_ [1];
		buffer_ [1] = temp;
		
	}

	void _createMaterial (ref Material mat_, Shader shader_) {
		
		if (mat_ == null) mat_ = new Material (shader_);
		
	}
	
	void _destroyMaterial (ref Material mat_) {
		
		if (mat_ != null) DestroyImmediate (mat_);
		
	}
	
	void OnGUI () {
		int  size = GUITextureSize;
		Rect r00  = new Rect (size * 0, size * 0, size, size);
		Rect r10  = new Rect (size * 1, size * 0, size, size);
		Rect r01  = new Rect (size * 0, size * 1, size, size);
		Rect r11  = new Rect (size * 1, size * 1, size, size);
		//Rect r02  = new Rect (size * 0, size * 2, size, size);
		//Rect r12  = new Rect (size * 1, size * 2, size, size);
		//Rect r03  = new Rect (size * 0, size * 3, size, size);
		//Rect r13  = new Rect (size * 1, size * 3, size, size);
		//Rect r04  = new Rect (size * 0, size * 4, size, size);
		//Rect r14  = new Rect (size * 1, size * 4, size, size);
		Rect r20  = new Rect (size * 2, size * 0, size, size);
		//Rect r21  = new Rect (size * 2, size * 1, size, size);
		Rect r30  = new Rect (size * 3, size * 0, size, size);
		Rect r31  = new Rect (size * 3, size * 1, size, size);

		//GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), _dyeBuffer [0]);

		GUI.DrawTexture (r00, _velocityBuffer [0]);
		GUI.Label (r00, "_velcotiyBuffer [0]");
		//GUI.DrawTexture (r01, _velocityBuffer [1]);
	//	GUI.Label (r01, "_velcotiyBuffer [1]");

		GUI.DrawTexture (r10, _pressureBuffer [0]);
		GUI.Label (r10, "_pressureBuffer [0]");
		//GUI.DrawTexture (r11, _pressureBuffer [1]);
	//	GUI.Label (r11, "_pressureBuffer [1]");

		//GUI.DrawTexture (r20, _divergenceBuffer);
		//GUI.Label (r20, "_divergenceBuffer");

		//GUI.DrawTexture (r30, _dyeBuffer [0]);
	//	GUI.Label (r30, "_dyeBuffer [0]");
		//GUI.DrawTexture (r31, _dyeBuffer [1]);
	//	GUI.Label (r31, "_dyeBuffer [1]");


	}
    public bool debugMouse;

    Vector4[] UpdatePosiiton() {
        Vector4[] forces = new Vector4[1000];
        for (int i = 0; i < 1000; i++)
        {
            if (i < TrackingObjects.Length)
            {

                if (!debugMouse) {

                    //Vector3 mp =TrackingObjects[i].position-origin.position;
                    Vector3 mp = Camera.main.WorldToScreenPoint(TrackingObjects[i].position);
                    print(mp);
                    //mp.x *= _aspectRatio;
                    Vector3 c = Camera.main.ScreenToViewportPoint(mp);
                    c.Scale(new Vector3(1, 1, 1));
                    forces[i] = new Vector4(c.x, c.y, c.z, 0);
            

                }
                else { 

                Vector3 mp = Input.mousePosition;
                    print(mp);
                    //mp.x *= _aspectRatio;
                    _currentMousePosition = Camera.main.ScreenToViewportPoint(mp);


                Vector3 c = _currentMousePosition;
                forces[i] = new Vector4(c.x, c.y, c.z, 0);
                }

            }
            else {

                forces[i] = new Vector4(0, 0, 0, 0);
            }
            //_particles[i].velocity;
        }

        return forces;
    }



    /*

    Vector4[]  UpdateParticalforce ( )
    {

        ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[psCursurs.particleCount];
        psCursurs.GetParticles(_particles);

        

           Vector4[] forces = new Vector4[1000];
        for (int i = 0; i < _particles.Length; i++)
        {

            Vector3 mp = Input.mousePosition;
            //mp.x *= _aspectRatio;
            _currentMousePosition = Camera.main.ScreenToViewportPoint(mp);


            Vector3 c = _currentMousePosition;
            forces[i] = new Vector4(c.x, c.y, c.z, 0); 
            //_particles[i].velocity;
        }

        return forces;
       
    }*/

}
