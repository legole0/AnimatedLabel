using System;
using Godot;
using System.Linq;
using Godot.Collections;

namespace AnimatedLabel;
#if TOOLS
[Tool, Icon("res://assets/ui/misc/AnimatedLabel.svg")]
#endif
[GlobalClass] public partial class AnimatedLabel : ReferenceRect
{
    [Export(PropertyHint.MultilineText)] public string Text
    {
        get => _text;
        set
        {
            _text = value;
            _letterArray = GetLetterArray();
            UpdateText();
        }
    }
    
    [Export(PropertyHint.File)] public AnimatedFont AnimatedFont
    {
        get => _animatedFont;
        set
        {
            _animatedFont = value;
            UpdateText();
        }
    }
    
    [ExportGroup("Animation"), Export(PropertyHint.Enum)] 
    private AnimationStyles AnimationStyles
    {
        get => _animationStyles;
        set
        {
            _animationStyles = value;
            UpdateText();
        }
    }
    
    [Export] public float SyncFrameSpeed
    {
        get => _syncFrameSpeed;
        set
        {
            _syncFrameSpeed = value;
            _syncFrameDuration = 1 / _syncFrameSpeed;
        }
    }

    [ExportGroup("Style"), Export] public float Separation
    {
        get => _separation;
        set
        {
            _separation = value;
            UpdateText();
        }
    }
    
    [Export] public float FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            UpdateText();
        }
    }
    
    [Export] public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            _horizontalAlignment = value;
            UpdateText();
        }
    }
    
    [Export] public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            _verticalAlignment = value;
            UpdateText();
        }
    }

    private string _text;
    private AnimatedFont _animatedFont;
    private AnimationStyles _animationStyles = AnimationStyles.Synchronized;
    private float _separation = 24f;
    private float _fontSize = 24f;
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
    
    private AnimatedLetter[] _letterArray;
    private float _syncFrameSpeed = 24f;
    private float _syncFrameLength;
    private int _syncFrameIndex;
    private float _syncFrameDuration;
    private float _timePassed;

    public override void _Ready()
    {
        UpdateText();
    }

    private AnimatedLetter[] GetLetterArray()
    {
        string[] splitText = Text.Select(x => x.ToString()).ToArray();
        AnimatedLetter[] letterArray = new AnimatedLetter[splitText.Length];
        for (int i = 0; i < splitText.Length; i++)
        {
            string letter = splitText[i];
            AnimatedLetter animatedLetter = new AnimatedLetter(letter);
            letterArray[i] = animatedLetter;
        }
        return letterArray;
    }

    private void UpdateLetterPositions(bool redraw = true)
    {
        for (int i = 0; i < _letterArray.Length; i++)
        {
            AnimatedLetter animLetter = _letterArray[i];
            
            // fallback rect for spaces or texture-less letters
            Vector2 position = new Vector2(FontSize + Separation * i, 0);
            Vector2 size = new Vector2(FontSize, FontSize);
            
            // figure out the actual position and scale if it's a valid letter
            if (animLetter.Texture != null && animLetter.Texture[0] != null)
            {
                Vector2 texSize = animLetter.Texture[0].GetSize();
                float texRatio = CalculateTextureRatio(texSize);
                
                size = new Vector2(texSize.X * texRatio, texSize.Y * texRatio);
                position = new Vector2(texSize.X*i, FontSize - size.Y);
            }
            
            animLetter.Rect = new Rect2(position, size);
        }
        CustomMinimumSize = new Vector2(Separation * _letterArray.Length + FontSize, FontSize);
        if (redraw)
            QueueRedraw();
    }

    private float CalculateTextureRatio(Vector2 texSize)
    {
        float widthRatio = FontSize / texSize.X;
        float heightRatio = FontSize / texSize.Y;
        float ratio = Math.Min(widthRatio, heightRatio);
        
        return ratio;
    }
    
    private void UpdateText()
    {
        foreach (AnimatedLetter animLetter in _letterArray)
        {
            if (AnimatedFont == null || AnimatedFont.SpriteFrames == null)
            {
                GD.PrintErr("The provided AnimatedFont is null");
                return;
            }

            if (!AnimatedFont.SpriteFrames.HasAnimation(animLetter.Letter))
                continue;
            
            //GD.Print(animLetter.Letter);
            int frameCount = AnimatedFont.SpriteFrames.GetFrameCount(animLetter.Letter);
            //GD.Print($"Frame count {frameCount}");
            animLetter.Texture = new Texture2D[frameCount];
            UpdateLetterPositions(false);
            if((frameCount > 1 && frameCount < _syncFrameLength) || _syncFrameLength <= 0)
                _syncFrameLength = frameCount;
                
            for (int frame = 0; frame < frameCount; frame++)
            {
                Texture2D frameTexture = AnimatedFont.SpriteFrames.GetFrameTexture(animLetter.Letter, frame);
                animLetter.Texture[frame] = frameTexture;
                
                if (AnimationStyles == AnimationStyles.InstantLoop)
                    animLetter.FrameSpeed = (float)AnimatedFont.SpriteFrames.GetAnimationSpeed(animLetter.Letter);

                //GD.Print($"Texture {frameTexture} for frame {frame} of letter {animLetter.Letter}");
                
                /*if (frameTexture is AtlasTexture atlasTexture)
                {
                    animatedLetter.SourceRect[frame] = atlasTexture.Region;
                }*/
            }
            //GD.Print($"Done setting {SpriteFrames.GetFrameCount(animLetter.Letter)} textures for letter: {animLetter.Letter}");
        }
        
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (AnimatedLetter animLetter in _letterArray)
        {
            int finalIndex = AnimationStyles == AnimationStyles.Synchronized ? _syncFrameIndex : animLetter.FrameIndex;
            if (animLetter.Texture != null && animLetter.Texture[finalIndex] != null)
            {
                // note to self:
                // this doesn't take in account letters without texture so that might be an issue
                Texture2D frameTexture = animLetter.Texture[finalIndex];
                if (frameTexture is AtlasTexture letterAtlas)
                {
                    //GD.Print($"drawing AtlasTexture letter {animLetter.Letter}");
                    // AtlasTexture drawing
                    Texture2D atlas = letterAtlas.Atlas;
                    Rect2 sourceRect = letterAtlas.Region;
                    DrawTextureRectRegion(atlas,
                        animLetter.Rect,
                        sourceRect,
                        Modulate
                        );
                    
                    continue;
                }
                //GD.Print($"drawing Texture2D letter {animLetter.Letter}");
                // Drawing other Texture2D derivatives
                DrawTextureRect(frameTexture,
                    animLetter.Rect,
                    false,
                    Modulate);
            }
        }
    }
    
    public override void _Process(double delta)
    {
        if (AnimationStyles != AnimationStyles.Synchronized)
            return;
        
        _timePassed += (float)delta;
        
        if (_syncFrameIndex > _syncFrameLength)
        {
            _syncFrameIndex = 0;
            _timePassed = 0;
        }
        
        _syncFrameIndex = GetCurrentFrame();
    }

    public int GetCurrentFrame()
    {
        int timeToFrame = (int)(_timePassed / _syncFrameDuration);
        GD.Print("frame: "+timeToFrame);
        GD.Print("length: "+_syncFrameLength);
        GD.Print("passed: "+_timePassed);
        GD.Print("duration: "+_syncFrameDuration);
        return timeToFrame;
    }
}
