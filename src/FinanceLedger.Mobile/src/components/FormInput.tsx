import React from 'react';
import { StyleSheet, View } from 'react-native';
import { HelperText, TextInput } from 'react-native-paper';
import type { TextInputProps } from 'react-native-paper';

interface FormInputProps extends Omit<TextInputProps, 'error'> {
  label: string;
  error?: string;
}

/** TextInput + inline validation error, consistent across forms. */
export function FormInput({ label, error, style, ...rest }: FormInputProps) {
  return (
    <View style={styles.wrapper}>
      <TextInput
        label={label}
        mode="outlined"
        error={Boolean(error)}
        style={[styles.input, style]}
        {...rest}
      />
      {error ? (
        <HelperText type="error" visible>
          {error}
        </HelperText>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    marginBottom: 4,
  },
  input: {
    backgroundColor: 'transparent',
  },
});
