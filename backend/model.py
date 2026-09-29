from transformers import CLIPProcessor, CLIPModel
from paths import MODEL_PATH

# MODEL_PATH resolves correctly whether running as a raw script or frozen executable
print("Loading CLIP model...")
model = CLIPModel.from_pretrained(MODEL_PATH)
processor = CLIPProcessor.from_pretrained(MODEL_PATH)
model.eval()
print("Model loaded.")
