﻿namespace Interlude

open OpenTK
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL
open Interlude.Render

module Game = 
    let version = "v0.4.0"

type Game() =
    inherit GameWindow(300, 300, new GraphicsMode(ColorFormat(32), 24, 8, 0))

    do
        base.Title <- "Interlude " + Game.version
        base.VSync <- VSyncMode.Off
        //base.Cursor <- new MouseCursor.Empty

    override this.OnResize (e) =
        base.OnResize (e)
        GL.Viewport(base.ClientRectangle)
        Render.resize(float base.Width, float base.Height)

    override this.OnRenderFrame (e) =
        base.OnRenderFrame (e)
        Render.start()
        Render.resize(float base.Width, float base.Height)
        Draw.rect(Rect.create 0.f 0.f (float32 base.Width) (float32 base.Height) |> Rect.expand(-20.0f, -20.0f)) Color.Aquamarine
        Render.finish()
        base.SwapBuffers()

    override this.OnUpdateFrame (e) =
        base.OnUpdateFrame (e)
    
    override this.OnLoad (e) =
        base.OnLoad(e)
        Render.init(float base.Width, float base.Height)

    override this.OnUnload (e) =
        base.OnUnload(e)