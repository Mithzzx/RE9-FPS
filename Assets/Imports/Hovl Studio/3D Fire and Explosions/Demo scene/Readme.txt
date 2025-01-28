This Asset can be used for commercial purposes if you purchased it in the "Asset store" from the seller "Hovl Studio".
All that is in the folder "3D Fire and Explosions" can be used in commerce, even demo scene files.
-----------------------------------------------------

If you want to use post-effect like in the demo video:

Enable post-effect bloom from Package manager post-processing. Or you can use your own post-processing.

Using:
1) Shaders
1.1)The "Use depth" on the material from the custom shaders is the Soft Particle Factor.
1.2)Use "Center glow"[MaterialToggle] only with particle system. This option is used to darken the main texture with a white texture (white is visible, black is invisible).
    If you turn on this feature, you need to use "Custom vertex stream" (Uv0.Custom.xy) in tab "Render". And don't forget to use "Custom data" parameters in your PS.
1.3)The distortion shader only works with standard rendering. Delete (if exist) distortion particles from effects if you use LWRP or HDRP!
1.4)You can change the cutoff in all shaders (except Add_CenterGlow and Blend_CenterGlow ) using (Uv0.Custom.xy) in particle system.

2)Light.
2.1)You can disable light in the main effect component (delete light and disable light in PS). 
    Light strongly loads the game if you don't use light probes or something else.

3)Quality
3.1) For better sparks quality enable "Anisotropic textures: Forced On" in quality settings.

  SUPPORT ASSET FOR URP or HDRP here --> URP and HDRP patches folder

Contact me if you have any questions.
My email: gorobecn2@gmail.com


Thank you for reading, I really appreciate it.
Please rate this asset in the Asset Store ^-^