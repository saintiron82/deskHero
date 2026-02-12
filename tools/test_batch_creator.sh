#!/bin/bash
# Test script for monster batch creator
# Demonstrates how to create a sample batch using the Python tool

echo "🧪 Testing Monster Batch Creator"
echo "================================="
echo ""

# Test case: Create Batch 2 with volcanic theme
echo "📝 Test Case: Batch 2 - Volcanic Wasteland"
echo ""

python tools/monster_batch_creator.py \
  --batch 2 \
  --theme "Volcanic Wasteland" \
  --name-ko "화산 지대" \
  --name-en "Volcanic Wasteland" \
  --species \
    lava_golem \
    fire_drake \
    magma_elemental \
    obsidian_warrior \
    flame_imp \
    ash_wraith \
    volcanic_spider \
    pyroclast \
    cinder_bat \
    inferno_wolf \
    magma_serpent \
    ember_skeleton \
    coal_demon

echo ""
echo "✅ Batch file created at: config/monsters/batch_02.json"
echo ""

# Validate JSON syntax
echo "🔍 Validating JSON syntax..."
python -c "import json; data = json.load(open('config/monsters/batch_02.json')); print(f'✅ Valid JSON: {len(data[\"monsters\"])} monsters × 6 elements = {len(data[\"monsters\"]) * 6} total')"

echo ""
echo "📊 Next steps:"
echo "  1. Run ba_ma for balance validation"
echo "  2. Add sprite images to Assets/Images/Production/"
echo "  3. Test in game: dotnet run"
echo ""
