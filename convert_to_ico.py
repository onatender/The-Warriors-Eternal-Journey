from PIL import Image
import os

def convert_png_to_ico(input_path, output_path):
    try:
        img = Image.open(input_path)
        # ICO içinde farklı boyutlar olması iyidir.
        icon_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
        
        # Eğer resim RGBA değilse çevir
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
            
        img.save(output_path, format='ICO', sizes=icon_sizes)
        print(f"Başarılı: {input_path} -> {output_path}")
    except Exception as e:
        print(f"Hata: {e}")

base_dir = r"c:\Users\onate\OneDrive\Desktop\The Warrior's Eternal Journey\EternalJourney"
logo_png = os.path.join(base_dir, "Content", "game_logo.png")
icon_ico = os.path.join(base_dir, "Icon.ico")

convert_png_to_ico(logo_png, icon_ico)
