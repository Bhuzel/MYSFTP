package com.yoursftp.app.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

// YoursFTP v1.0.0 Luxury Dark Palette
val LuxuryBg = Color(0xFF0A0A0B)
val LuxuryBgSoft = Color(0xFF121213)
val LuxurySurface = Color(0xFF19191B)
val LuxurySurface2 = Color(0xFF222224)
val LuxuryBorder = Color(0xFF2B2B2E)
val LuxuryBorderSoft = Color(0xFF1F1F21)
val LuxuryText = Color(0xFFD9D4C7)
val LuxuryTextMuted = Color(0xFF8B877C)
val LuxuryTextDim = Color(0xFF5C5952)
val LuxuryAccent = Color(0xFFCDBD94)
val LuxuryAccentStrong = Color(0xFFDED0AA)
val LuxuryAccentInk = Color(0xFF17150F)
val LuxurySuccess = Color(0xFF7FBF8F)
val LuxuryCodeBg = Color(0xFF101011)

private val DarkColors = darkColorScheme(
    primary = LuxuryAccent,
    onPrimary = LuxuryAccentInk,
    primaryContainer = LuxurySurface2,
    onPrimaryContainer = LuxuryAccentStrong,
    secondary = LuxuryAccentStrong,
    onSecondary = LuxuryAccentInk,
    secondaryContainer = LuxurySurface,
    onSecondaryContainer = LuxuryText,
    background = LuxuryBg,
    onBackground = LuxuryText,
    surface = LuxurySurface,
    onSurface = LuxuryText,
    surfaceVariant = LuxurySurface2,
    onSurfaceVariant = LuxuryTextMuted,
    outline = LuxuryBorder,
    outlineVariant = LuxuryBorderSoft
)

private val LightColors = DarkColors // Always adhere to the curated luxury dark aesthetic

@Composable
fun YoursFtpTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = DarkColors,
        content = content
    )
}
