# 阿米娅模组卡图复制脚本
# 根据IMPLEMENTATION_PLAN.md的映射表，将阿米娅卡图中的图片复制并按卡牌类名重命名

param(
    [string]$sourceDir = "..\阿米娅卡图",
    [string]$targetDir = "Amiya\images\cards"
)

# 卡牌类名到图号的映射
$mapping = @{
    # 1.png → 10张卡牌
    "strike" = 1
    "emotion_absorption" = 1
    "truth_remnant" = 1
    "return" = 1
    "running_night" = 1
    "tactical_bombardment" = 1
    "support_future" = 1
    "behead" = 1
    "return_to_beginning" = 1
    "soul_tenacity_song" = 1
    # 2.png → 13张卡牌
    "resolve" = 2
    "one_strike_ends_all" = 2
    "storm_assault" = 2
    "roaring_light" = 2
    "abandon_abyss" = 2
    "sword_never_sleeps" = 2
    "red_dawn_draw" = 2
    "rage_whirlwind" = 2
    "pain_connection" = 2
    "blood_blade_suspended" = 2
    "long_spear_night_fire" = 2
    "untuned_rage" = 2
    "unattainable_desire" = 2
    # 3.png → 1张
    "rabbit_kick" = 3
    # 4.png → 16张卡牌
    "tactical_chant" = 4
    "defend" = 4
    "form_switch" = 4
    "insight_eye" = 4
    "grief_empathy" = 4
    "armor_up" = 4
    "tomorrow_again" = 4
    "pursuit" = 4
    "sculpture_installation" = 4
    "horizon_fire_cloud" = 4
    "bone_voice" = 4
    "see_through" = 4
    "good_night_dying" = 4
    "borrow_tomorrow" = 4
    "storm_watch" = 4
    "phase_change" = 4
    "show_me_truth" = 4
    "cyan_rage" = 4
    # 5.png → 12张
    "turn_back_smile" = 5
    "graceful_heart" = 5
    "body_as_wall" = 5
    "see_what_i_see" = 5
    "believe_tomorrow" = 5
    "broken_horn" = 5
    "act_ordinary" = 5
    "dance_in_flowers" = 5
    "hello" = 5
    "where_to_go" = 5
    "my_heart_follows" = 5
    "mercy_vision" = 5
    # 6.png → 12张
    "demon_lord_sword" = 6
    "endless_flood" = 6
    "blood_for_blood" = 6
    "black_crown_shock" = 6
    "survival_omen" = 6
    "time_has_come" = 6
    "cannot_refuse" = 6
    "demon_lord_vessel" = 6
    "demon_lord_banner" = 6
    "prophecy_image" = 6
    "arrived" = 6
    "final_shadow" = 6
    # 7.png → 1张
    "thousand_visions" = 7
    # 8.png → 1张
    "see_light" = 8
    # 9.png → 1张
    "her" = 9
    # 10.png → 13张
    "ordinary_joy" = 10
    "wildfire" = 10
    "refuse_mourn" = 10
    "soul_fortress" = 10
    "iron_guard" = 10
    "turbulence" = 10
    "nursing_card" = 10
    "void_fragment" = 10
    "today_tomorrow_deviation" = 10
    "spread" = 10
    "sowing" = 10
    "buried_underground" = 10
    "wild_roar" = 10
}

foreach ($card in $mapping.GetEnumerator()) {
    $sourceFile = Join-Path $sourceDir "$($card.Value).png"
    $targetFile = Join-Path $targetDir "$($card.Name).png"
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile $targetFile -Force
        Write-Host "Copied: $($card.Key).png ← $($card.Value).png"
    } else {
        Write-Warning "Source not found: $sourceFile"
    }
}

Write-Host "Done! Total cards: $($mapping.Count)"
