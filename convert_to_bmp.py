from PIL import Image
import os

def convert_png_to_bmp(input_path, output_path, target_size=None):
    try:
        img = Image.open(input_path)
        
        # Inno Setup için genelde 164x314 (WizardImage) veya 55x58 (WizardSmallImage) önerilir.
        # Ancak otomatik resize yapmak yerine sadece formatı çevirelim, Inno Setup sığdırır.
        # Eğer boyut argumenti gelirse resize yapalım.
        
        if target_size:
             img = img.resize(target_size, Image.Resampling.LANCZOS)

        # BMP 24-bit kaydet
        if img.mode != 'RGB':
            img = img.convert('RGB')
            
        img.save(output_path, 'BMP')
        print(f"Başarılı: {input_path} -> {output_path}")
    except Exception as e:
        print(f"Hata: {e}")

# Yollar (Absolute paths kullanmak daha güvenli ama script çalıştığı dizine göre relative de olur)
base_dir = r"c:\Users\onate\OneDrive\Desktop\The Warrior's Eternal Journey\EternalJourney"
logo_png = os.path.join(base_dir, "Content", "game_logo.png")

# 1. WizardImage (Ana Görsel) - game_logo.png'yi BMP'ye çevir
# Bu görsel genelde 164x314 px civarında olur ama büyükse de Inno Setup kesebilir.
# Biz garanti olsun diye BMP yapalım, boyutu orijinal kalsın (veya 164x314'e zorlayabiliriz)
# Kullanıcının "game_logo.png"si büyük ihtimalle kare veya yatay. 
# WizardImage dikey bir alandır (sol taraf).
# Yine de direkt çevirelim.
convert_png_to_bmp(logo_png, os.path.join(base_dir, "Content", "game_logo.bmp"))

# 2. WizardSmallImage (Sağ üst ikon) - Icon.ico veya game_logo kullanılabilir.
# game_logo'yu küçültüp 55x55 yapalım small image için
convert_png_to_bmp(logo_png, os.path.join(base_dir, "Content", "game_logo_small.bmp"), (55, 58))

print("Dönüştürme tamamlandı.")
