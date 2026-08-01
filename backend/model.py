from transformers import CLIPProcessor, CLIPModel
from paths import MODEL_PATH

# Note: __file__-based paths are unreliable once frozen by PyInstaller,
# since the loader doesn't always resolve it to a real path next to the exe.
# paths.MODEL_PATH handles the frozen vs. dev-script distinction correctly.

print("Loading CLIP model...")
model = CLIPModel.from_pretrained(MODEL_PATH)
processor = CLIPProcessor.from_pretrained(MODEL_PATH)
model.eval()
print("Model loaded.")