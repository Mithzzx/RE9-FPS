All classes contain summary.

CharacterDamagePainter setup:
1. Add CharacterDamagePainter component to object
2. Add renderers with different materials to ProjectingRenderers
3. Add renderers with same material (same with ProjectingRenderer) to SetuppingRenderers for each ProjectingRenderer
4. Set TexturePropertyName _DamageMask
5. Set Texture Resolution (512, 1024 or 2048)
6. Set ZDepth 1
7. Set Brushes count 1
8. Set brush size 0.1 (it is camera size)
9. Set brush texture (some black and white mask texture)
10. Setup to all renderers Knife/PBR Damageable shader and setup it like in example (blood doll damageable)
11. You should call Paint from other script (where you take damage) or use HittableDoll component

LiquidSurface setup:
1. Create Plane
2. Add LiquidSurface component to created plane
3. Setup Render Texture Creator in LiquidSurface component
4. Setup Rendering Camera Creator in LiquidSurface component
5. Setup Camera placing in LiquidSurface component
6. Setup Particles Pool in LiquidSurface component
7. Setup Texture Applier in LiquidSurface component

All example settings you can look in Showcase scene (pool)

Shaders.

Blood Puddle - used for showing random puddles.
	- Power and MaxDistance - masking parameters
	- ShowFraction - show by mask fraction
	- Power2 and Power3 - noise mask powers
	- Mul - noise multiplier
	- ColumnsRows - atlas columns and rows count
	- FrameNoise - sprite (from atlas) randomness
	- Albedo, Normals, Specular - regular PBR textures
	- Noise - noise texture
	- Smoothness - specular smoothness multiplier
	- Tint - color
	- NormalScale - power of normal map
	- SpecularTint - specular add color
	- Alpha - transparency
	
Blood Trail - used for trails
	- Albedo, Normal - regular PBR textures
	- Specular - specular color
	- NormalScale - power of normalmap
	- Smoothness - specular smoothness multiplier
	
Distrotion - simple distortion shader
	- MainTex, NormalMap - regular PBR textures
	- NormalScale - power of normal map
	- Distortion - amount of distortion
	- Alpha - transparency
	- Frame - frame from atlas
	- Columns - atlas columns count
	- Rows - atlas rows count
	- SubstractAlphaFading - enables alternative alpha fading method, instead of multiplication shader will use substraction
	- DistortionAlphaPower - power of alpha distortion
	- SpecularNormalMul - influence normal map to fake specular reflections
	- ReflectionMap - fake ambient cubemap for fake reflections
	- Specular - intensity of reflections
	- AlphaPower - power of alpha
	
Liquid Blood - useful for texture atlas animation
	- Has regular PBR parameters and fake reflections parameters
	- Tint and Tint2 - 2 colors that create gradient from zero alpha to one alpha
	- AlphaRemapColorBlend - alpha remapping for gradient
	- AlphaRemap - alpha remapping for alpha blending
	- FadeDistance - soft particles blend distance
	- Cull - face culling

Liquid Blood Errosion - useful for particles
	- Has regular PBR parameters and fake reflections parameters
	- Errosion - amount of soft "cutout" fading
	- VelocityErrosion - amount of errosion by particle velocity
	- You can control errosion and velocity errosion by particle system custom vertex streams (Custom1.xy - TEXCOORD0.zw)
	
Liquid Blood Sphere Errosion - used for mesh particles (sphere)
	- Same as Blood Errosion shader, but have other parameters
	- Scale - scale of sphere
	- Tessellation
	- DisplacementNoise texture
	- MaxDisplacement - amount of displacement
	
PBR Damageable - damageable character shader
	- Has regular PBR parameters
	- Has second layer - damage layer (Albedo, Normal, Specular etc.)
	- DamagerBorderLength, DamagerBorderPower - damage border length and softness parameters
	- DamageMaskPower - power of damage mask texture
	- DamageDisplacement - amount of displacement by damage mask
	
PBR Damageable Metallic - same as PBR Damageable but has metallic instead of specular
	
Liquid Surface Opaque - simple liquid surface shader
	- Color - color of surface
	- Tesellation
	- SpecularColor - specular color of surface
	- Displacement - amount of displacement by mask
	- Mask - mask
	- NormalOffset, NormalStrength - normal create (from mask) parameters to create dynamic normals
	
Liquid Surface Transparent
	- Same as Liquid Surface Opaque but transparent and has other parameters
	- Foam texture - this texture drawed on small distance between surface and object under liquid surface
	- FoamDepth and FoamBorderDepth - foam blending parameters
	- FoamAmplitude and FoamFrequenncy - foam animation parameters
	- DepthColor - color of further object under liquid surface
	- Distrotion - distortion amount
	- Normal 1 and Normal 2 - used for static distortion animation
	- Static Normal - enables static distortion normals