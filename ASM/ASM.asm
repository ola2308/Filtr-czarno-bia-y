.data
    align 16
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0

.code
GrayscaleFilter PROC
    ; Parametry:
    ; rcx - wskaŸnik do inputBuffer
    ; rdx - wskaŸnik do outputBuffer
    ; r8d - pixelCount (int)
    ; xmm3 - brightness (float)

    push rbp
    mov rbp, rsp
    push rsi
    push rdi
    
    mov rsi, rcx                    ; Input buffer
    mov rdi, rdx                    ; Output buffer
    mov ecx, r8d                    ; Pixel count
    
    ; Przygotuj wspó³czynniki
    movups xmm0, [rgb_weights]      ; Za³aduj wagi RGB
    shufps xmm3, xmm3, 0           ; Rozszerz brightness na wszystkie elementy
    mulps xmm0, xmm3               ; Przemnó¿ wagi przez jasnoœæ
    
    ; Przygotuj maski do przetwarzania wektorowego
    movaps xmm4, xmm0              ; Wagi dla B (0.299)
    movaps xmm5, xmm0              ; Wagi dla G (0.587)
    movaps xmm6, xmm0              ; Wagi dla R (0.114)
    shufps xmm4, xmm4, 0          ; Broadcast wagi B
    shufps xmm5, xmm5, 01010101b  ; Broadcast wagi G
    shufps xmm6, xmm6, 10101010b  ; Broadcast wagi R
    
    ; Wyzeruj rejestr do rozszerzania bajtów
    pxor xmm7, xmm7
    
    ; Przetwarzaj 4 piksele na raz
    shr ecx, 2                     ; Liczba pe³nych bloków 4-pikselowych
    test ecx, ecx
    jz cleanup
    
process_pixels:
    ; Za³aduj 12 bajtów (4 piksele RGB)
    movd xmm1, dword ptr [rsi]     ; Za³aduj pierwsze 4 bajty
    movd xmm2, dword ptr [rsi+4]   ; Za³aduj kolejne 4 bajty
    movd xmm3, dword ptr [rsi+8]   ; Za³aduj ostatnie 4 bajty
    
    ; Rozpakuj bajty do s³ów
    punpcklbw xmm1, xmm7           ; Rozszerz B komponenty
    punpcklbw xmm2, xmm7           ; Rozszerz G komponenty
    punpcklbw xmm3, xmm7           ; Rozszerz R komponenty
    
    ; Rozpakuj s³owa do dwordów
    punpcklwd xmm1, xmm7           ; B do float
    punpcklwd xmm2, xmm7           ; G do float
    punpcklwd xmm3, xmm7           ; R do float
    
    ; Konwersja na float
    cvtdq2ps xmm1, xmm1            ; Konwertuj B
    cvtdq2ps xmm2, xmm2            ; Konwertuj G
    cvtdq2ps xmm3, xmm3            ; Konwertuj R
    
    ; Oblicz wartoœci szaroœci (równolegle dla 4 pikseli)
    mulps xmm1, xmm4               ; B * waga_B
    mulps xmm2, xmm5               ; G * waga_G
    mulps xmm3, xmm6               ; R * waga_R
    
    ; Sumuj komponenty
    addps xmm1, xmm2               ; Dodaj G
    addps xmm1, xmm3               ; Dodaj R
    
    ; Konwersja z powrotem na inty z zaokr¹gleniem
    cvtps2dq xmm1, xmm1
    
    ; Spakuj z powrotem do bajtów z saturacj¹
    packssdw xmm1, xmm1            ; Konwersja do word z saturacj¹
    packuswb xmm1, xmm1            ; Konwersja do byte z saturacj¹
    
    ; Zapisz wyniki dla wszystkich 4 pikseli
    movd eax, xmm1
    
    ; Zapisz wyniki z replikacj¹ dla RGB
    mov byte ptr [rdi], al         ; Piksel 1
    mov byte ptr [rdi+1], al
    mov byte ptr [rdi+2], al
    shr eax, 8
    mov byte ptr [rdi+3], al       ; Piksel 2
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    shr eax, 8
    mov byte ptr [rdi+6], al       ; Piksel 3
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    shr eax, 8
    mov byte ptr [rdi+9], al       ; Piksel 4
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al
    
    ; Przesuñ wskaŸniki
    add rsi, 12                    ; Przesuñ wskaŸnik wejœciowy (4 piksele * 3 bajty)
    add rdi, 12                    ; Przesuñ wskaŸnik wyjœciowy
    
    dec ecx
    jnz process_pixels

cleanup:
    pop rdi
    pop rsi
    pop rbp
    ret
GrayscaleFilter ENDP
END