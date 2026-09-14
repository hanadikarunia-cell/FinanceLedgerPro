import React, { useState } from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
  View,
} from 'react-native';
import { Button, Text } from 'react-native-paper';
import { SafeAreaView } from 'react-native-safe-area-context';
import { FormInput } from '@/components/FormInput';
import { useAuth } from '@/auth/AuthContext';
import { palette } from '@/theme';

export function LoginScreen() {
  const { login, isLoggingIn, error } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  const onSubmit = async () => {
    setLocalError(null);
    if (!email.trim() || !password) {
      setLocalError('Email and password are required.');
      return;
    }
    try {
      await login({ email: email.trim(), password });
    } catch {
      // error surfaced from context below
    }
  };

  return (
    <SafeAreaView style={styles.safe}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.flex}
      >
        <View style={styles.container}>
          <View style={styles.header}>
            <Text variant="displaySmall" style={styles.brand}>
              Finance Ledger Pro
            </Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
              Sign in to continue
            </Text>
          </View>

          <FormInput
            label="Email"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
            autoComplete="email"
          />
          <FormInput
            label="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoComplete="password"
          />

          {(localError || error) ? (
            <Text style={styles.error}>{localError ?? error}</Text>
          ) : null}

          <Button
            mode="contained"
            style={styles.button}
            loading={isLoggingIn}
            disabled={isLoggingIn}
            onPress={onSubmit}
          >
            Sign In
          </Button>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: palette.slate },
  flex: { flex: 1 },
  container: {
    flex: 1,
    justifyContent: 'center',
    padding: 24,
    backgroundColor: '#fff',
    margin: 16,
    borderRadius: 16,
  },
  header: { alignItems: 'center', marginBottom: 32 },
  brand: { fontWeight: '800', color: palette.slate, textAlign: 'center' },
  subtitle: { opacity: 0.6, marginTop: 8 },
  error: { color: palette.expense, marginTop: 8, textAlign: 'center' },
  button: { marginTop: 24, paddingVertical: 4 },
});
