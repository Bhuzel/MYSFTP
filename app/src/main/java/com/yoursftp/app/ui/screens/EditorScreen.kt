package com.yoursftp.app.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.rememberTransformableState
import androidx.compose.foundation.gestures.transformable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Save
import androidx.compose.material.icons.filled.ZoomIn
import androidx.compose.material.icons.filled.ZoomOut
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.yoursftp.app.editor.PrecomputedHighlightTransformation
import com.yoursftp.app.editor.buildHighlightedText
import com.yoursftp.app.ui.EditorViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EditorScreen(
    vm: EditorViewModel,
    path: String,
    onBack: () -> Unit
) {
    val state by vm.state.collectAsState()
    val snackbar = remember { SnackbarHostState() }

    val verticalScrollState = rememberScrollState()
    val horizontalScrollState = rememberScrollState()

    var fontSizeSp by remember { mutableStateOf(13f) }
    val transformableState = rememberTransformableState { zoomChange, _, _ ->
        fontSizeSp = (fontSizeSp * zoomChange).coerceIn(8f, 30f)
    }

    LaunchedEffect(path) { vm.load(path) }

    LaunchedEffect(state.savedMessage, state.error) {
        (state.savedMessage ?: state.error)?.let {
            snackbar.showSnackbar(it)
            vm.clearMessages()
        }
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbar) },
        topBar = {
            TopAppBar(
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = Color(0xFF19191B), // Luxury Surface
                    titleContentColor = Color(0xFFD9D4C7),
                    navigationIconContentColor = Color(0xFFCDBD94),
                    actionIconContentColor = Color(0xFFCDBD94)
                ),
                title = {
                    Text(
                        path.substringAfterLast('/'),
                        maxLines = 1, overflow = TextOverflow.Ellipsis,
                        fontFamily = FontFamily.Monospace,
                        fontSize = 15.sp,
                        fontWeight = FontWeight.Bold
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Kembali")
                    }
                },
                actions = {
                    // Zoom Buttons
                    IconButton(onClick = { fontSizeSp = (fontSizeSp - 1f).coerceAtLeast(8f) }) {
                        Icon(Icons.Default.ZoomOut, contentDescription = "Perkecil", modifier = Modifier.size(18.dp))
                    }
                    IconButton(onClick = { fontSizeSp = (fontSizeSp + 1f).coerceAtMost(30f) }) {
                        Icon(Icons.Default.ZoomIn, contentDescription = "Perbesar", modifier = Modifier.size(18.dp))
                    }
                    IconButton(onClick = { vm.save() }, enabled = !state.saving && !state.loading) {
                        Icon(Icons.Default.Save, contentDescription = "Simpan", tint = Color(0xFFCDBD94))
                    }
                }
            )
        }
    ) { padding ->
        Box(
            Modifier
                .fillMaxSize()
                .padding(padding)
                .background(Color(0xFF101011)) // Luxury Code Background
                .transformable(transformableState)
        ) {
            if (state.loading) {
                CircularProgressIndicator(Modifier.align(Alignment.Center), color = Color(0xFFCDBD94))
            } else {
                val highlighted by produceState(
                    initialValue = AnnotatedString(state.content),
                    state.content, path
                ) {
                    value = withContext(Dispatchers.Default) {
                        buildHighlightedText(state.content, path)
                    }
                }
                val transformation = remember(highlighted) {
                    PrecomputedHighlightTransformation(highlighted)
                }

                Column(Modifier.fillMaxSize()) {
                    Row(
                        modifier = Modifier
                            .weight(1f)
                            .verticalScroll(verticalScrollState)
                    ) {
                        // Line numbers column
                        val lineCount = remember(state.content) { state.content.split('\n').size }
                        val lineNumbersText = remember(lineCount) { (1..lineCount).joinToString("\n") }
                        
                        Column(
                            modifier = Modifier
                                .background(Color(0xFF121213))
                                .padding(vertical = 12.dp)
                                .widthIn(min = 40.dp)
                        ) {
                            Text(
                                text = lineNumbersText,
                                style = TextStyle(
                                    fontFamily = FontFamily.Monospace,
                                    fontSize = fontSizeSp.sp,
                                    lineHeight = (fontSizeSp * 1.5f).sp,
                                    color = Color(0xFF5C5952)
                                ),
                                modifier = Modifier.padding(horizontal = 8.dp)
                            )
                        }

                        // Gutter separator line
                        Box(
                            modifier = Modifier
                                .fillMaxHeight()
                                .width(1.dp)
                                .background(Color(0xFF1F1F21))
                        )

                        // Text Field with horizontal scrolling
                        Box(
                            modifier = Modifier
                                .weight(1f)
                                .horizontalScroll(horizontalScrollState)
                                .padding(vertical = 12.dp)
                        ) {
                            BasicTextField(
                                value = state.content,
                                onValueChange = vm::onContentChange,
                                textStyle = TextStyle(
                                    fontFamily = FontFamily.Monospace,
                                    fontSize = fontSizeSp.sp,
                                    lineHeight = (fontSizeSp * 1.5f).sp,
                                    color = Color(0xFFD9D4C7)
                                ),
                                cursorBrush = SolidColor(Color(0xFFCDBD94)),
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(horizontal = 12.dp),
                                visualTransformation = transformation
                            )
                        }
                    }

                    // Bottom Status Bar
                    Surface(
                        color = Color(0xFF19191B),
                        contentColor = Color(0xFFCDBD94),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier.padding(horizontal = 12.dp, vertical = 5.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            val lineCount = remember(state.content) { state.content.split('\n').size }
                            val charCount = remember(state.content) { state.content.length }
                            val fileExt = path.substringAfterLast('.', "").uppercase()
                            
                            Text(
                                text = "Baris: $lineCount | Karakter: $charCount | Skala: ${(fontSizeSp * 100 / 13).toInt()}%",
                                fontSize = 11.sp,
                                color = Color(0xFF8B877C),
                                fontFamily = FontFamily.Monospace
                            )
                            Text(
                                text = if (fileExt.isNotEmpty()) fileExt else "TEXT",
                                fontSize = 11.sp,
                                fontWeight = FontWeight.Bold,
                                color = Color(0xFFDED0AA),
                                fontFamily = FontFamily.Monospace
                            )
                        }
                    }
                }
            }
            if (state.saving) {
                CircularProgressIndicator(Modifier.align(Alignment.TopEnd).padding(16.dp))
            }
        }
    }
}
